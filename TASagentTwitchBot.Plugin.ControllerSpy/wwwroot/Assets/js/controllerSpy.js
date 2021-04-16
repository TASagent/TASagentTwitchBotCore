let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/ControllerSpy")
    .build();

connection.on('ControllerUpdate',
    function (state) {
        //Draw Buttons

        ctx.clearRect(0, 0, cw, ch);

        DrawLeftArrow(state.left);
        DrawRightArrow(state.right);
        DrawUpArrow(state.up);
        DrawDownArrow(state.down);
        DrawA(state.a);
        DrawB(state.b);
        DrawX(state.x);
        DrawY(state.y);

        if (state.r) {
            DrawR();
        }

        if (state.l) {
            DrawL();
        }
    });

connection.start();

// now we will setup our basic variables for the demo
var controllerSpyCanvas = document.getElementById('controllerSpyCanvas'),
    ctx = controllerSpyCanvas.getContext('2d'),
    // dimensions
    cw = window.innerWidth,
    ch = window.innerHeight;

// set canvas dimensions
controllerSpyCanvas.width = cw;
controllerSpyCanvas.height = ch;

var mainHeightRegion = 0.85 * ch;
var widthSectionRegion = cw / 3;

var squareSize = Math.min(mainHeightRegion, widthSectionRegion);
var fullHeight = squareSize / 0.85;
var lrHeight = 0.5 * (fullHeight - squareSize);


var sq1x0 = cw / 2 - 1.5 * squareSize;
var sq1x0c = sq1x0 + 0.5 * squareSize;
var sq2x0 = sq1x0 + squareSize;
var sq3x0 = sq2x0 + squareSize;
var sq3xc = sq3x0 + 0.5 * squareSize;

var sqy0 = ch / 2 - 0.5 * fullHeight + lrHeight;
var sqy0c = sqy0 + 0.5 * squareSize;
var sqy1 = sqy0 + squareSize;

function DrawLeftArrow(pressed) {
    ctx.beginPath();

    ctx.moveTo(sq1x0c - 0.05 * squareSize, sqy0c);
    ctx.lineTo(sq1x0c - 0.05 * squareSize - squareSize / 6, sqy0c - squareSize / 6);
    ctx.lineTo(sq1x0, sqy0c - squareSize / 6);
    ctx.lineTo(sq1x0, sqy0c + squareSize / 6);
    ctx.lineTo(sq1x0c - 0.05 * squareSize - squareSize / 6, sqy0c + squareSize / 6);
    ctx.lineTo(sq1x0c - 0.05 * squareSize, sqy0c);
    ctx.lineTo(sq1x0c - 0.05 * squareSize - squareSize / 6, sqy0c - squareSize / 6);

    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#091421";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#09142150";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawRightArrow(pressed) {
    ctx.beginPath();

    ctx.moveTo(sq1x0c + 0.05 * squareSize, sqy0c);
    ctx.lineTo(sq1x0c + 0.05 * squareSize + squareSize / 6, sqy0c + squareSize / 6);
    ctx.lineTo(sq2x0, sqy0c + squareSize / 6);
    ctx.lineTo(sq2x0, sqy0c - squareSize / 6);
    ctx.lineTo(sq1x0c + 0.05 * squareSize + squareSize / 6, sqy0c - squareSize / 6);
    ctx.lineTo(sq1x0c + 0.05 * squareSize, sqy0c);
    ctx.lineTo(sq1x0c + 0.05 * squareSize + squareSize / 6, sqy0c + squareSize / 6);

    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#091421";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#09142150";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawDownArrow(pressed) {
    ctx.beginPath();

    ctx.moveTo(sq1x0c, sqy0c + 0.05 * squareSize);
    ctx.lineTo(sq1x0c + squareSize / 6, sqy0c + squareSize / 6 + 0.05 * squareSize);
    ctx.lineTo(sq1x0c + squareSize / 6, sqy1);
    ctx.lineTo(sq1x0c - squareSize / 6, sqy1);
    ctx.lineTo(sq1x0c - squareSize / 6, sqy0c + squareSize / 6 + 0.05 * squareSize);
    ctx.lineTo(sq1x0c, sqy0c + 0.05 * squareSize);
    ctx.lineTo(sq1x0c + squareSize / 6, sqy0c + squareSize / 6 + 0.05 * squareSize);

    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#091421";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#09142150";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawUpArrow(pressed) {
    ctx.beginPath();

    ctx.moveTo(sq1x0c, sqy0c - 0.05 * squareSize);
    ctx.lineTo(sq1x0c + squareSize / 6, sqy0c - squareSize / 6 - 0.05 * squareSize);
    ctx.lineTo(sq1x0c + squareSize / 6, sqy0);
    ctx.lineTo(sq1x0c - squareSize / 6, sqy0);
    ctx.lineTo(sq1x0c - squareSize / 6, sqy0c - squareSize / 6 - 0.05 * squareSize);
    ctx.lineTo(sq1x0c, sqy0c - 0.05 * squareSize);
    ctx.lineTo(sq1x0c + squareSize / 6, sqy0c - squareSize / 6 - 0.05 * squareSize);

    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#091421";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#09142150";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawA(pressed) {
    ctx.beginPath();
    ctx.arc(sq3xc + 0.25 * squareSize, sqy0c, 0.16 * squareSize, 0, 2 * Math.PI);
    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#8a0a7e";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#8a0a7e50";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawB(pressed) {
    ctx.beginPath();
    ctx.arc(sq3xc, sqy0c + 0.25 * squareSize, 0.16 * squareSize, 0, 2 * Math.PI);
    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#8a0a7e";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#8a0a7e50";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawX(pressed) {
    ctx.beginPath();
    ctx.arc(sq3xc, sqy0c - 0.25 * squareSize, 0.16 * squareSize, 0, 2 * Math.PI);
    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#d49fcf";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#d49fcf50";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawY(pressed) {
    ctx.beginPath();
    ctx.arc(sq3xc - 0.25 * squareSize, sqy0c, 0.16 * squareSize, 0, 2 * Math.PI);
    ctx.lineWidth = 5;

    if (pressed) {
        ctx.fillStyle = "#d49fcf";
        ctx.strokeStyle = "#FFFFFF";
    }
    else {
        ctx.fillStyle = "#d49fcf50";
        ctx.strokeStyle = "#FFFFFF50";
    }

    ctx.fill();
    ctx.stroke();
}

function DrawL() {
    ctx.beginPath();
    ctx.moveTo(sq1x0, sqy0 - 0.05 * lrHeight);
    ctx.lineTo(sq1x0 + squareSize, sqy0 - 0.05 * lrHeight);
    ctx.lineTo(sq1x0 + squareSize, sqy0 - lrHeight);
    ctx.lineTo(sq1x0, sqy0 - lrHeight);
    ctx.lineTo(sq1x0, sqy0 - 0.05 * lrHeight);

    ctx.lineWidth = 5;
    ctx.fillStyle = "#ccd8e6";
    ctx.fill();
    ctx.strokeStyle = "#FFFFFF";
    ctx.stroke();
}

function DrawR() {
    ctx.beginPath();
    ctx.moveTo(sq3x0, sqy0 - 0.05 * lrHeight);
    ctx.lineTo(sq3x0 + squareSize, sqy0 - 0.05 * lrHeight);
    ctx.lineTo(sq3x0 + squareSize, sqy0 - lrHeight);
    ctx.lineTo(sq3x0, sqy0 - lrHeight);
    ctx.lineTo(sq3x0, sqy0 - 0.05 * lrHeight);

    ctx.lineWidth = 5;
    ctx.fillStyle = "#ccd8e6";
    ctx.fill();
    ctx.strokeStyle = "#FFFFFF";
    ctx.stroke();
}

function Clear() {
    ctx.clearRect(0, 0, cw, ch);
}

window.onload = Clear;