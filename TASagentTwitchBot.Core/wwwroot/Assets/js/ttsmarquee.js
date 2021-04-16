
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/Hubs/TTSMarquee")
    .build();

connection.on('ReceiveTTSNotification',
    function (message) {
        $('.marquee').html(message).marquee().on('finished', function () {
            $('.marquee').html('');
            setTimeout(WaitAndShow, 500);
        });
    });

connection.start();