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

module reactor {

    export class WriteStream {
        private xhr    : XMLHttpRequest
        private offset : number

        constructor(public endpoint: string) {
            this.xhr    = new XMLHttpRequest()
            this.offset = 0
        }

        public write(array: Uint8Array, callback?: () => void) : void {
            this.xhr.onreadystatechange = () => {
                if(this.xhr.readyState == 1) {
                    var start : string =  this.offset.toString()
                    var end   : string = (this.offset + array.byteLength).toString()
                    this.xhr.setRequestHeader('Range', ['bytes=', start, '-', end].join('') )
                }

                if (this.xhr.readyState == 4) {
                    if(this.xhr.status == 200) {
                        this.offset += array.byteLength
                        if(callback) { callback() }
                        return
                    }

                    setTimeout(() => {
                        this.write(array, callback)
                    }, 4000)
                }
            }

            this.xhr.open('POST', this.endpoint, true)
            this.xhr.send(array)
        }

        public end(callback?: () => void): void {
            if(callback) {
                callback()
            }
        }
    }
}