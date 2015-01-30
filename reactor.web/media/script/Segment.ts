/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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
    export interface Progress {
        sent    : number
        total   : number
        percent : number
        scalar  : number
    }
    export class Segment {
        public onprogress: (progress:Progress) => void
        constructor(public endpoint : string,
                    public file     : File,
                    public start    : number,
                    public end      : number) {
        
        }
        public process(callback:(error: Error) => void) : void {
            this.read((error, array) => {
                if(error) {
                    callback(error)
                    return
                }
                this.write(array, callback)    
            })
        }
        private write(array: Uint8Array, callback: (error: Error) => void) : void {
            var xhr = new XMLHttpRequest()
            xhr.upload.onprogress = (e:any) => {
                if(this.onprogress) {
                    var sent = (e.loaded + this.start)
                    this.onprogress({
                        sent    : sent,
                        total   : this.file.size,
                        percent : Math.round((sent * 100) / this.file.size),
                        scalar  : (sent / this.file.size)
                    })
                }
            }
            xhr.onreadystatechange = () => {
                if(xhr.readyState == 1) {
                    xhr.setRequestHeader('Range', ['bytes=', this.start, '-', this.end].join('') )
                }
                if (xhr.readyState == 4) {
                    if(xhr.status == 200) {
                        callback(null)
                        return
                    }
                    callback(new Error('unable to send chunk ' + this.start + '-' + this.end))    
                }
            }
            xhr.open('POST', this.endpoint, true)
            xhr.send(array)
        }
        private read(callback: (error: Error, array: Uint8Array) => void) : void {
            var reader = new FileReader()
            reader.onloadend = (e: any) => {
                if (e.target.readyState == 2) {
                    var array = new Uint8Array(e.target.result)
                    callback(null, array)
                }
            }
            var blob = this.file.slice(this.start, this.end)
            reader.readAsArrayBuffer(blob)
        }
    }
}