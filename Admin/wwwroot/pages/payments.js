"use strict";

var page = {
    init: function () {
        ! function () {
            const t = document.getElementById("tb_data");

            if (!t) return;

            const e = $(t).DataTable({
                info: 1,
                order: [1],
                ajax: t.getAttribute('data-url'),
                columnDefs: [{
                    targets: 6,
                    orderable: !1
                }],
                "oLanguage": {
                    "sEmptyTable": `
                        <div class="text-center p-15"><img src="/admin/img/illustrations/empty-cart.png" class="mh-100px">
                            <span class="font-weight-bold d-block">There are no records to show</span>
                        </div>`
                },
                buttons: [{
                    extend: 'excelHtml5',
                    autoFilter: true,
                    sheetName: 'Exported data'
                }],
                createdRow: function (row, data, index) {
                    $(row).attr('id', `tr_${data[0]}`);

                    $('td', row).eq(3).html(
                        `<span id='b_${data[0]}' class="badge badge-${data[14]}">${data[3]}</span>`);

                    $('td', row).eq(6).addClass('text-end');
                    $('td', row).eq(6).html(
                        `<a href="javascript:;" id="a_${data[0]}" 
                            class="btn btn-sm btn-light btn-active-light-primary a_view_payment"
                            onclick="fnn('a_${data[0]}')"
                            data-id="${data[0]}"
                            data-name="${data[7]}" 
                            data-amount="${data[2]}" 
                            data-status="${data[3]}" 
                            data-gateway="${data[4]}" 
                            data-remarks="${data[6]}" 
                            data-item="${data[10]}" 
                            data-email="${data[8]}" 
                            data-phone="${data[9]}" 
                            data-currency="${data[11]}" 
                            data-date="${data[5]}" 
                            data-timeago="${data[13]}" 
                            data-css="${data[14]}" 
                            data-paidto="${data[15]}" 
                            data-payref="${data[16]}" 
                            data-confirm="${data[17]}" 
                            data-description="${data[1]}">
                            <span class="fw-bolder">View</span>
                        </a>`);
                }
            });

            var r = document.getElementById("tb_search");

            r && r.addEventListener("keyup", (function (t) {
                e.search(t.target.value).draw();
            }));

        }()
    }
};

KTUtil.onDOMContentLoaded((function () {
    page.init()
}));

var fnn = function (id) {
    var current = document.getElementById(id);

    $('#a_confirm').addClass('d-none');

    $('#pay_description').html(current.getAttribute('data-description'));
    $('#pay_id').html(current.getAttribute('data-id'));
    $('#pay_amount').html(current.getAttribute('data-amount'));
    $('#pay_date').html(current.getAttribute('data-date'));
    $('#pay_status').html(current.getAttribute('data-status'));
    $('#pay_remarks').html(current.getAttribute('data-remarks'));
    $('#pay_status').html(current.getAttribute('data-status'));
    $('#pay_timeago').html(current.getAttribute('data-timeago'));
    $('#pay_date').html(current.getAttribute('data-date'));
    $('#pay_name').html(current.getAttribute('data-name'));
    $('#pay_email').html(current.getAttribute('data-email'));
    $('#pay_phone').html(current.getAttribute('data-phone'));
    $('#pay_paidto').html(current.getAttribute('data-paidto'));
    $('#pay_payref').html(current.getAttribute('data-payref'));

    $('#pay_status').removeClass('badge-success');
    $('#pay_status').removeClass('badge-danger');
    $('#pay_status').removeClass('badge-warning');
    $('#pay_status').removeClass('badge-light');
    $('#pay_status').addClass(`badge-${current.getAttribute('data-css')}`);

    if (current.getAttribute('data-confirm') == '1') {
        $('#a_confirm').removeClass('d-none');
        $('#a_confirm').attr('data-id', current.getAttribute('data-id'));

        $('#a_cancel').removeClass('d-none');
        $('#a_cancel').attr('data-id', current.getAttribute('data-id'));
    }

    $('#diag_details').modal('show');
}

$('#a_confirm').click(function () {

    var c = $(this);
    var id = c.attr('data-id');
    var url = `${c.attr('data-url')}/${id}`;
    var current = document.getElementById(`a_${id}`);

    Swal.fire({
        html: `Are you sure you want to confirm the payment <span class="fw-bolder">#${id}</span>
               of <span class="fw-boldest">₦${current.getAttribute('data-amount')}</span>
               from <span class="fw-boldest">${current.getAttribute('data-name')}?</span>
               You cannot reverse this.`,
        icon: "question",
        buttonsStyling: false,
        showCancelButton: true,
        confirmButtonText: "Yes, Confirm",
        cancelButtonText: 'No, Cancel',
        customClass: {
            confirmButton: "btn btn-primary",
            cancelButton: 'btn btn-danger'
        }
    }).then((value) => {

        if (value.isConfirmed) {

            c.attr("data-kt-indicator", "on");

            $.ajax({
                type: "PUT",
                url: url,
                cache: false,
                success: function (json) {
                    current.setAttribute('data-status', json.status);
                    current.setAttribute('data-css', 'success');

                    $('#pay_status').html(json.status);
                    $('#pay_status').removeClass('badge-success');
                    $('#pay_status').removeClass('badge-danger');
                    $('#pay_status').removeClass('badge-warning');
                    $('#pay_status').removeClass('badge-light');
                    $('#pay_status').addClass('badge-success');

                    $(`#b_${id}`).html(json.status);
                    $(`#b_${id}`).removeClass('badge-success');
                    $(`#b_${id}`).removeClass('badge-danger');
                    $(`#b_${id}`).removeClass('badge-warning');
                    $(`#b_${id}`).removeClass('badge-light');
                    $(`#b_${id}`).addClass('badge-success');

                    c.addClass('d-none');
                    $('#a_cancel').addClass('d-none');

                    toastr.success(`Update was successful for payment #${id}`);
                },
                error: function (error) {
                    toastr.error(error.responseText);
                },
                complete: function () {
                    c.attr("data-kt-indicator", "off");
                }
            });

        }

    });;

});

$('#a_cancel').click(function () {

    var c = $(this);
    var id = c.attr('data-id');
    var url = `${c.attr('data-url')}/${id}`;
    var current = document.getElementById(`a_${id}`);

    Swal.fire({
        html: `Are you sure you want to cancel the payment <span class="fw-bolder">#${id}</span>
               of <span class="fw-boldest">₦${current.getAttribute('data-amount')}</span>
               from <span class="fw-boldest">${current.getAttribute('data-name')}?</span>
               You cannot reverse this.`,
        icon: "question",
        buttonsStyling: false,
        showCancelButton: true,
        confirmButtonText: "Yes, Cancel",
        cancelButtonText: 'No, Cancel',
        customClass: {
            confirmButton: "btn btn-danger",
            cancelButton: 'btn btn-info'
        }
    }).then((value) => {

        if (value.isConfirmed) {

            c.attr("data-kt-indicator", "on");

            $.ajax({
                type: "PUT",
                url: url,
                cache: false,
                success: function (json) {
                    current.setAttribute('data-status', json.status);
                    current.setAttribute('data-css', 'success');

                    $('#pay_status').html(json.status);
                    $('#pay_status').removeClass('badge-success');
                    $('#pay_status').removeClass('badge-danger');
                    $('#pay_status').removeClass('badge-warning');
                    $('#pay_status').removeClass('badge-light');
                    $('#pay_status').addClass('badge-danger');

                    $(`#b_${id}`).html(json.status);
                    $(`#b_${id}`).removeClass('badge-success');
                    $(`#b_${id}`).removeClass('badge-danger');
                    $(`#b_${id}`).removeClass('badge-warning');
                    $(`#b_${id}`).removeClass('badge-light');
                    $(`#b_${id}`).addClass('badge-danger');

                    c.addClass('d-none');
                    $('#a_confirm').addClass('d-none');

                    toastr.success(`Update was successful for payment #${id}`);
                },
                error: function (error) {
                    toastr.error(error.responseText);
                },
                complete: function () {
                    c.attr("data-kt-indicator", "off");
                }
            });

        }

    });;

});