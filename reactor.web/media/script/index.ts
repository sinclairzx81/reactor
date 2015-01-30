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

/// <reference path="Upload.ts" />
/// <reference path="Segment.ts" />

module reactor {

    export function segment(file:File, endpoint:string, size:number) : reactor.Segment [] {
        var segments = []
        var index    = 0
        for(var i = 0; i < Math.floor(file.size / size); i++) {
            segments.push(new reactor.Segment(endpoint, 
                                              file, 
                                              index,
                                              index + size))
            index += size
        }
        var remainder = file.size % size
        if(remainder > 0) {
            segments.push(new reactor.Segment(endpoint, 
                                              file, 
                                              index,
                                              index + remainder))
        }
        return segments;          
    }

    export function upload(file: File, endpoint: string, size?: number) : reactor.Upload {

        size = size || 1048576 * 10

        return new reactor.Upload( segment(file, endpoint, size) )
    }
}