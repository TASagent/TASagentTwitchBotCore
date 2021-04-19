let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/Emote")
    .build();

connection.on('ReceiveEmotes',
    function (urls) {
        urls.forEach(function (url) {
            pendingEmotes.push(new Emote(url));
          });
    });

connection.start();

window.requestAnimFrame = (function () {
    return window.requestAnimationFrame ||
        window.webkitRequestAnimationFrame ||
        window.mozRequestAnimationFrame ||
        function (callback) {
            window.setTimeout(callback, 1000 / 60);
        };
})();

var emoteCanvas = document.getElementById('emoteCanvas'),
    emoteCtx = emoteCanvas.getContext('2d'),
    pendingEmotes = [],
    emotes = [],
    cw = window.innerWidth,
    ch = window.innerHeight;

emoteCanvas.width = cw;
emoteCanvas.height = ch;

class Emote {
    constructor(url) {
        this.x = Random(0, cw - 75);
        this.y = -75;

        this.imageUrl = url;

        this.speed = 2;
        this.acceleration = 1.08;

        this.image = new Image();
        this.image.src = url;
    }
    
    Update(index) {
        this.speed *= this.acceleration;
        this.y += this.speed;

        if (this.y > ch + 75) {
            emotes.splice(index, 1);
        }
    }

    Draw() {
        emoteCtx.drawImage(this.image, this.x, this.y, 75, 75);
    }
}

function Random(min, max) {
    return Math.random() * (max - min) + min;
}

function Loop() {
    requestAnimFrame(Loop);

    emoteCtx.clearRect(0, 0, cw, ch);

    if (pendingEmotes.length > 0)
    {
        emotes.push(pendingEmotes.splice(0,1)[0]);
    }

    var i = emotes.length;
    while (i--) {
        emotes[i].Draw();
        emotes[i].Update(i);
    }
}

window.onload = Loop;