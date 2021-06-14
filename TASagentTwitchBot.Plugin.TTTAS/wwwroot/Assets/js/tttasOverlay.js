let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/TTTAS")
    .build();

connection.on('ReceivePrompt',
    function (word) {
        overlayText = "Say: " + word;
        active = true;
    });

connection.on('ClearPrompt',
    function () {
        pendingClear = true;
        active = false;
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

var tttasCanvas = document.getElementById('tttasCanvas'),
    ctx = tttasCanvas.getContext('2d'),
    cw = window.innerWidth,
    ch = window.innerHeight;

var pendingClear = false;
var active = false;

// set canvas dimensions
tttasCanvas.width = cw;
tttasCanvas.height = ch;

var mainHeightRegion = 0.85 * ch;
var widthSectionRegion = cw / 3;

function Clear()
{
    ctx.clearRect(0, 0, cw, ch);
}

function Loop()
{
    requestAnimFrame(Loop);

    if (pendingClear)
    {
        Clear();
        pendingClear = false;
    }

    if (!active)
    {
        return;
    }

    DrawText();
}


function DrawText()
{
    var fontSize = 120;
    ctx.clearRect(0, 0, cw, ch);
    ctx.globalCompositeOperation = 'source-over';
    ctx.font = fontSize + "px Futura, Helvetica, sans-serif";

    // calculate width + height of text-block
    var metrics = ctx.measureText(overlayText);
    var offsetX = (cw - metrics.width) / 2;
    var offsetY = (ch - fontSize) / 2;

    //Render Prompt
    ctx.lineWidth = 6;
    ctx.strokeStyle = "rgba(255,255,255,1)";
    ctx.fillStyle = "rgba(0,0,255,1)";
    ctx.shadowOffsetX = 0;
    ctx.shadowOffsetY = 0;
    ctx.shadowBlur = 0;
    ctx.strokeText(overlayText, offsetX, offsetY + fontSize);
    ctx.fillText(overlayText, offsetX, offsetY + fontSize);
};

window.onload = Loop;