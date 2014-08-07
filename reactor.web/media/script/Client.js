var reactor;
(function (reactor) {
    var WriteStream = (function () {
        function WriteStream(endpoint) {
            this.endpoint = endpoint;
            this.xhr = new XMLHttpRequest();
            this.offset = 0;
        }
        WriteStream.prototype.write = function (array, callback) {
            var _this = this;
            this.xhr.onreadystatechange = function () {
                if (_this.xhr.readyState == 1) {
                    var start = _this.offset.toString();
                    var end = (_this.offset + array.byteLength).toString();
                    _this.xhr.setRequestHeader('Range', ['bytes=', start, '-', end].join(''));
                }

                if (_this.xhr.readyState == 4) {
                    if (_this.xhr.status == 200) {
                        _this.offset += array.byteLength;
                        if (callback) {
                            callback();
                        }
                        return;
                    }

                    setTimeout(function () {
                        _this.write(array, callback);
                    }, 4000);
                }
            };

            this.xhr.open('POST', this.endpoint, true);
            this.xhr.send(array);
        };

        WriteStream.prototype.end = function (callback) {
            if (callback) {
                callback();
            }
        };
        return WriteStream;
    })();
    reactor.WriteStream = WriteStream;
})(reactor || (reactor = {}));
var reactor;
(function (reactor) {
    var ReadStream = (function () {
        function ReadStream(file, options) {
            this.file = file;
            this.options = options;
            var default_chunk_size = 1048576 * 10;
            if (!this.options) {
                this.options = {
                    chunksize: default_chunk_size
                };
            }

            if (!this.options.chunksize) {
                this.options.chunksize = default_chunk_size;
            }

            this.reader = new FileReader();
            this.chunks = [];
            this.paused = false;
            this.reading = false;
            this.closed = false;

            var index = 0;
            for (var i = 0; i < Math.floor(this.file.size / this.options.chunksize); i++) {
                this.chunks.push({ start: index, end: index + this.options.chunksize });
                index += this.options.chunksize;
            }

            var remainder = this.file.size % this.options.chunksize;
            if (remainder > 0) {
                this.chunks.push({ start: index, end: index + remainder });
            }
        }
        ReadStream.prototype.pipe = function (writeable) {
            var _this = this;
            this.ondata = function (buffer) {
                _this.pause();
                writeable.write(buffer, function () {
                    _this.resume();
                });
            };

            this.onend = function () {
                writeable.end();
            };

            this.resume();
        };

        ReadStream.prototype.pause = function () {
            this.paused = true;
        };

        ReadStream.prototype.resume = function () {
            this.paused = false;
            if (!this.reading) {
                if (!this.closed) {
                    this.read();
                }
            }
        };

        ReadStream.prototype.read = function () {
            var _this = this;
            this.reading = true;
            var chunk = this.chunks.shift();
            this._read(chunk, function (buffer) {
                _this.reading = false;
                if (_this.ondata) {
                    _this.ondata(buffer);
                }
                if (_this.chunks.length == 0) {
                    _this.closed = true;
                    if (_this.onend) {
                        _this.onend();
                    }
                    return;
                }
                if (!_this.paused) {
                    if (!_this.closed) {
                        _this.read();
                    }
                }
            });
        };

        ReadStream.prototype._read = function (chunk, callback) {
            this.reader.onloadend = function (e) {
                if (e.target.readyState == 2) {
                    callback(new Uint8Array(e.target.result));
                }
            };
            var blob = this.file.slice(chunk.start, chunk.end);
            this.reader.readAsArrayBuffer(blob);
        };
        return ReadStream;
    })();
    reactor.ReadStream = ReadStream;
})(reactor || (reactor = {}));
