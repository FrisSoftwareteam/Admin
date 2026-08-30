"use strict";
var data_page = {
	init_datatable_client: function () {
		! function () {
			const t = document.getElementById("tb_client");
			if (!t) return;
			const e = $(t).DataTable({
				info: !1,
				order: [],
				columnDefs: [{
					targets: 0,
					orderable: !1
				}]
			});
			var r = document.getElementById("tb_search");
			r && r.addEventListener("keyup", (function (t) {
				e.search(t.target.value).draw()
			}))
		}()
	}
};

KTUtil.onDOMContentLoaded((function () {
	data_page.init_datatable_client()
}));

$('.form-loader').submit(function (e) {
    var button = this.getElementsByClassName('bt-submit')[0];
    button.setAttribute("data-kt-indicator", "on");
});

$('.bt_loader').click(function () {
    var button = $(this);
    button.setAttribute("data-kt-indicator", "on");
});

/*full modal*/

$('[data-mode=full-modal]').on('click', function (e) {
    e.preventDefault();
    openFullModal($(this).attr('data-target'));
});

var openFullModal = function (id) {
    $(`#${id}`).fadeIn();
    //$('body').css('overflow', 'hidden');
}

$('[data-dismiss=full-modal]').on('click', function (e) {
    e.preventDefault();
    var dis = $(this).closest('.full-modal');
    dis.fadeOut();
    $('body').css('overflow', 'auto');
});

$('.a_reset').on('click', function () {

    let userid = this.getAttribute('data-id');
    let name = this.getAttribute('data-name');
    let email = this.getAttribute('data-email');

    let blockUI = new KTBlockUI(document.querySelector(`#tr_${userid}`));
    blockUI.block();

    $.ajax({
        type: "GET",
        url: `/admin/forgot/${email}`,
        data: {
            id: userid,
        },
        cache: false,
        success: function () {
            toastr.success(`A new reset link has been generated for ${name} and sent to ${email}`);
        },
        error: function () {
            toastr.error('could not update status');
        },
        complete: function () {
            blockUI.release();
            blockUI.destroy();
        }
    });

});

function copyToClipboard(text) {
    navigator.clipboard.writeText(text)
        .then(() => {
            toastr.info("text copied");
        })
        .catch(err => {
            toastr.error("text could not be copied");
        });
}