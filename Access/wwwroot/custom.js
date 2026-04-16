$('.load-go').click(function () {
    $(this).html('Loading... <span class="spinner-border spinner-border-sm align-middle ms-2"></span>');
});

$('.form-go').submit(function () {
    $(":submit").html('Loading... <span class="spinner-border spinner-border-sm align-middle ms-2"></span>');
});

$('.form-loader').submit(function () {
    var button = document.querySelector(".bt-submit");
    button.setAttribute("data-kt-indicator", "on");
});

$('.bt-loader').click(function () {
    var button = $(this);
    button.setAttribute("data-kt-indicator", "on");
});

Inputmask({
    "mask": "99/99/9999"
}).mask("[data-mask=date]");

//$('[data-mode=full-modal]').click(function (e) {
//    e.preventDefault();
//    $(`#${$(this).attr('data-target')}`).fadeIn();
//    $('body').css('overflow', 'hidden');
//});

/*full modal*/

$('[data-mode=full-modal]').on('click', function (e) {
    e.preventDefault();
    openFullModal($(this).attr('data-target'));
});

var openFullModal = function (id) {
    $(`#${id}`).fadeIn();
    //$('body').css('overflow', 'hidden');
}

var switchBlock = function (showBlock, blockClass) {
    $(`.${blockClass}`).css('display', 'none');
    $(`#${showBlock}`).css('display', 'block');
}

$('[data-dismiss=full-modal]').click(function (e) {
    e.preventDefault();
    var dis = $(this).closest('.full-modal');
    dis.fadeOut();
    $('body').css('overflow', 'auto');
});

if ($('#rpt_holdings').length && typeof $.fn.repeater === 'function') {
    $('#rpt_holdings').repeater({
        initEmpty: false,

        defaultValues: {
            'text-input': 'foo'
        },

        show: function () {
            $(this).slideDown();
        },

        hide: function (deleteElement) {
            $(this).slideUp(deleteElement);
        }
    });
}