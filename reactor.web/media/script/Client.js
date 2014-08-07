var reactor;
(function (reactor) {
    var Segment = (function () {
        function Segment(endpoint, file, start, end) {
            this.endpoint = endpoint;
            this.file = file;
            this.start = start;
            this.end = end;
        }
        Segment.prototype.process = function (callback) {
            var _this = this;
            this.read(function (error, array) {
                if (error) {
                    callback(error);
                    return;
                }
                _this.write(array, callback);
            });
        };
        Segment.prototype.write = function (array, callback) {
            var _this = this;
            var xhr = new XMLHttpRequest();
            xhr.upload.onprogress = function (e) {
                if (_this.onprogress) {
                    var sent = (e.loaded + _this.start);
                    _this.onprogress({
                        sent: sent,
                        total: _this.file.size,
                        percent: Math.round((sent * 100) / _this.file.size),
                        scalar: (sent / _this.file.size)
                    });
                }
            };
            xhr.onreadystatechange = function () {
                if (xhr.readyState == 1) {
                    xhr.setRequestHeader('Range', ['bytes=', _this.start, '-', _this.end].join(''));
                }
                if (xhr.readyState == 4) {
                    if (xhr.status == 200) {
                        callback(null);
                        return;
                    }
                    callback(new Error('unable to send chunk ' + _this.start + '-' + _this.end));
                }
            };
            xhr.open('POST', this.endpoint, true);
            xhr.send(array);
        };
        Segment.prototype.read = function (callback) {
            var reader = new FileReader();
            reader.onloadend = function (e) {
                if (e.target.readyState == 2) {
                    var array = new Uint8Array(e.target.result);
                    callback(null, array);
                }
            };
            var blob = this.file.slice(this.start, this.end);
            reader.readAsArrayBuffer(blob);
        };
        return Segment;
    })();
    reactor.Segment = Segment;
})(reactor || (reactor = {}));
var reactor;
(function (reactor) {
    var Upload = (function () {
        function Upload(segments) {
            this.segments = segments;
            this.running = false;
            this.paused = true;
            this.index = 0;
        }
        Upload.prototype.restart = function () {
            this.index = 0;
            this.resume();
        };
        Upload.prototype.resume = function () {
            this.paused = false;
            if (this.index < this.segments.length) {
                if (!this.running) {
                    this.process();
                }
            }
        };
        Upload.prototype.pause = function () {
            this.paused = true;
        };
        Upload.prototype.process = function () {
            var _this = this;
            var action = function () {
                var segment = _this.segments[_this.index];
                segment.onprogress = _this.onprogress;
                _this.running = true;
                segment.process(function (error) {
                    _this.running = false;
                    if (error) {
                        if (_this.onerror) {
                            _this.onerror(error);
                        }
                        return;
                    }
                    _this.index = _this.index + 1;
                    if (_this.index < _this.segments.length) {
                        if (!_this.paused) {
                            action();
                        }
                    } else {
                        if (_this.oncomplete) {
                            _this.oncomplete();
                        }
                    }
                });
            };
            action();
        };
        return Upload;
    })();
    reactor.Upload = Upload;
})(reactor || (reactor = {}));
var reactor;
(function (reactor) {
    function segment(file, endpoint, size) {
        var segments = [];
        var index = 0;
        for (var i = 0; i < Math.floor(file.size / size); i++) {
            segments.push(new reactor.Segment(endpoint, file, index, index + size));
            index += size;
        }
        var remainder = file.size % size;
        if (remainder > 0) {
            segments.push(new reactor.Segment(endpoint, file, index, index + remainder));
        }
        return segments;
    }
    reactor.segment = segment;

    function upload(file, endpoint, size) {
        size = size || 1048576 * 10;

        return new reactor.Upload(segment(file, endpoint, size));
    }
    reactor.upload = upload;
})(reactor || (reactor = {}));
