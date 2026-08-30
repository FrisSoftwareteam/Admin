"use strict";

$('#a_close').click(function () {

    var c = $(this);
    var id = c.attr('data-id');
    var code = c.attr('data-code');
    var url = `${c.attr('data-url')}/${id}`;
    var current = document.getElementById(`a_${id}`);

    Swal.fire({
        html: `Are you sure you want to close this ticket <span class="fw-bolder">#${code}</span>.`,
        icon: "question",
        buttonsStyling: false,
        showCancelButton: true,
        confirmButtonText: "Yes, Close",
        cancelButtonText: 'No, Cancel',
        customClass: {
            confirmButton: "btn btn-danger",
            cancelButton: 'btn btn-primary'
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