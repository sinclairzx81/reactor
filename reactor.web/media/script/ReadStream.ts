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

/// <reference path="IWriteStream.ts" />
/// <reference path="IReadStream.ts" />

module reactor {

    export interface IReadStreamOptions {

        chunksize : number
    }

    export class ReadStream implements IReadStream {
    
        private  reader     : FileReader
        private  chunks     : {start: number; end: number;}[]
        private  paused     : boolean
        private  reading    : boolean
        private  closed     : boolean
        public   ondata     : (array: Uint8Array) => void
        public   onend      : () => void

        constructor(public file: File, public options: IReadStreamOptions) {

            //-----------------------------
            // initialize options
            //-----------------------------

            var default_chunk_size = 1048576 * 10
            if(!this.options) {
                this.options = {
                    chunksize : default_chunk_size
                }
            }

            if(!this.options.chunksize)  {
                this.options.chunksize = default_chunk_size
            }

            //------------------------------
            // initialize state
            //------------------------------
            
            this.reader     = new FileReader()
            this.chunks     = []
            this.paused     = false
            this.reading    = false
            this.closed     = false

            //------------------------------
            // compute chunks indices
            //------------------------------
            var index       = 0
            for(var i = 0; i < Math.floor(this.file.size / this.options.chunksize); i++) {
                this.chunks.push({ start : index, end : index + this.options.chunksize })
                index += this.options.chunksize
            }

            var remainder = this.file.size % this.options.chunksize
            if(remainder > 0) {
                this.chunks.push({ start : index, end : index + remainder })
            }
        }

        public pipe(writeable: reactor.IWriteStream): void {

            this.ondata = (buffer) => {
                this.pause()
                writeable.write(buffer, () => {
                    this.resume()    
                })
            }

            this.onend = () => {
                writeable.end()
            };

            this.resume()
        }

        public pause(): void {
            this.paused = true
        }

        public resume(): void {
            this.paused = false
            if (!this.reading) {
                if (!this.closed) {     
                    this.read()
                }
            }
        }

        private read (): void {
            this.reading = true
            var chunk    = this.chunks.shift()
            this._read(chunk, (buffer) => {
                this.reading = false
                if(this.ondata) {
                    this.ondata(buffer)    
                }
                if(this.chunks.length == 0) {
                    this.closed = true
                    if(this.onend) {
                        this.onend()    
                    }
                    return
                }
                if(!this.paused) {
                    if(!this.closed) {
                        this.read()    
                    }    
                }
            })
        }

        private _read (chunk:{start: number; end: number;}, callback: (array: Uint8Array) => void): void {
            this.reader.onloadend = (e: any) => {
                if (e.target.readyState == 2) {
                    callback(new Uint8Array(e.target.result))
                }
            }
            var blob = this.file.slice(chunk.start, chunk.end)
            this.reader.readAsArrayBuffer(blob)
        }
    }
}