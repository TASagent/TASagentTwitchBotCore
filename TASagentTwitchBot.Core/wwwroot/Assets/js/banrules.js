(function () {
    let banRules = [];
    //input-banrule { regex: /^\D{2,6}[o0]f[s5]te[ea][1il]\d{3,}$/ }
    // get ban rules, assign unique id for each one's remove button
    const updateDisplayedBanRules = () => {
        $('#banrule-container').empty();
        banRules.forEach((val, index) => {
            $('#banrule-container').append(`<div id="container-banrule-${index}" class="input-group mb-2">
            <label class="input-group-text">${val.regex}</label>
            <label class="input-group-text">Text Type: ${val.textContentType}</label>
            <label class="input-group-text">Timeout Seconds: ${val.timeoutSeconds}</label>
            <label class="input-group-text">Timeout Cooldown: ${val.timeoutCooldown}</label>
            <label class="input-group-text">Show Message: ${val.showMessage}</label>
            <label class="input-group-text">Use Timeout: ${val.useTimeout}</label>

            <button class="btn btn-primary" id="button-rmrule-${index}">Remove</button>
            </div>`);
            $(`#button-rmrule-${index}`).click(function (e) {
                removeBanRule(banRules[index], () => {
                    banRules.splice(index, 1);
                    updateDisplayedBanRules();
                });
            });
        });
    };


    const getBanRules = () => {
        $.getJSON({
            url: "/TASagentBotAPI/Ban/GetBanRules",
            beforeSend: auth.ApplyAuth,
            success: (result) => {
                banRules = result;
                updateDisplayedBanRules();
            }
        });
    };


    const addBanRule = (rule, onSuccess) => {
        $.post({
            url: "/TASagentBotAPI/Ban/AddBanRule",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify(rule),
            beforeSend: auth.ApplyAuth,
            success: (data) => onSuccess(data),
            error: auth.HandleErrorResponse
        });
    };


    const removeBanRule = (rule, onSuccess) => {
        $.post({
            url: "/TASagentBotAPI/Ban/RemoveBanRule",
            headers: { "Authorization": "PASS NONE" },
            contentType: "application/json;charset=utf-8",
            data: JSON.stringify(rule),
            beforeSend: auth.ApplyAuth,
            success: onSuccess,
            error: auth.HandleErrorResponse
        });
    };

    const resetTab = () => {
        $('#input-banrule').val("");
        $('#select-messageType').val(0).change();
        $('#input-showmsg').val(false);
        $('#input-timeoutseconds').val(1);
        $('#input-timeout').val(true);
    };

    $(document).ready(() => {
        $("#button-addbanrule").click(() => {
            const rule = $('#input-banrule').val();
            const msgType = parseInt($('#select-messageType').val());
            const timeoutSeconds = parseInt($('#input-timeoutseconds').val());
            const timeoutCooldown = parseInt($('#input-timeoutcooldown').val());

            let useTimeout = $('#input-timeout').is(":checked");
            let showMessage = $('#input-showmsg').is(":checked");
            useTimeout = useTimeout.constructor === Boolean ?
                useTimeout : useTimeout === 'true' || useTimeout === 'on';
            showMessage = showMessage.constructor === Boolean ?
                showMessage : showMessage === 'true' || showMessage === 'on';
            if (rule) {
                const newRule = {
                    regex: rule,
                    textContentType: msgType,
                    timeoutSeconds,
                    timeoutCooldown,
                    showMessage,
                    useTimeout,
                };
                addBanRule(newRule, (data) => {
                    resetTab();
                    banRules.push(data);
                    updateDisplayedBanRules();
                });
            }
        });
        $('#nav-settings-banrules-tab').click(() => {
            resetTab();
            getBanRules();
        });
        $('#input-timeout').change(function () {
            if (this.checked) {
                $('#input-timeoutseconds').prop('disabled', false);
                $('#input-timeoutcooldown').prop('disabled', false);

                $('#container-timeout-seconds').show();
            }
            else {
                $('#input-timeoutseconds').prop('disabled', true);
                $('#input-timeoutcooldown').prop('disabled', true);

                $('#container-timeout-seconds').hide();
                $('#input-timeoutseconds').val(1);
                $('#input-timeoutcooldown').val(-1);
            }
        });
    });

})();