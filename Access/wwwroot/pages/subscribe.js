
$(document).ready(function () {
    var hdPrice = document.querySelector("#hd_price");
    var dialerElement = document.querySelector("#dial_years");
    var dialerObject = KTDialer.getInstance(dialerElement);

    dialer_updated(1, hdPrice.value);

    dialerObject.on('kt.dialer.increase', function () {
        dialer_updated(dialerObject.value + 1, hdPrice.value);
    });

    dialerObject.on('kt.dialer.decrease', function () {
        dialer_updated(dialerObject.value - 1, hdPrice.value);
    });
});

var dialer_updated = function (value, amount) {
    if (value > 0) {
        $('#hd_years').val(value);

        var amt = value * amount;
        $('#hd_amount').val(amt);
        $('.sp_amount').html(new Intl.NumberFormat('en-US', { style: 'currency', currency: 'NGN' }).format(amt));
    }
};
