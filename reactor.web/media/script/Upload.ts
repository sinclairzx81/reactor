/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2014 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

/// <reference path="Segment.ts" />

module reactor {

    export class Upload {
        public  onprogress   : (progress: reactor.Progress) => void
        public  onerror      : (error: Error) => void
        public  oncomplete   : () => void
        private running      : boolean
        private paused       : boolean
        private index        : number
        constructor(public segments:Segment[]) {
            this.running = false
            this.paused  = true
            this.index   = 0
        }
        public restart() : void {
            this.index = 0
            this.resume()
        }
        public resume() : void {
            this.paused = false
            if(this.index < this.segments.length) {
                if(!this.running) {
                    this.process()
                }
            }
        }
        public pause() : void {
            this.paused = true
        }
        private process() : void {
            var action = () => {
                var segment = this.segments[this.index]
                segment.onprogress = this.onprogress
                this.running = true
                segment.process((error) => {
                    this.running = false
                    if(error) {
                        if(this.onerror) { 
                            this.onerror(error)
                        }
                        return
                    }
                    this.index = this.index + 1
                    if(this.index < this.segments.length) {
                        if(!this.paused){
                            action()
                        }
                    } else {
                        if(this.oncomplete) {
                            this.oncomplete()
                        }
                    }
                })
            }
            action()
        }
    }
}