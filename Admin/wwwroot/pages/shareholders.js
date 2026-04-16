"use strict";

var page = {
    init: function () {
        ! function () {
            const t = document.getElementById("tb_data");

            if (!t) return;

            const e = $(t).DataTable({
                info: 1,
                processing: true,
                ajax: t.getAttribute('data-url'),
                columnDefs: [{
                    targets: 4,
                    orderable: !1
                }],
                language: {
                    processing: `<div class="d-flex align-items-center gap-3 py-4">
                        <span class="spinner-border spinner-border-sm text-primary" role="status"></span>
                        <span class="text-muted fw-semibold">Loading shareholders...</span>
                    </div>`,
                    emptyTable: `
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
                    $(row).attr('id', data[2]);

                    //if (data[1] == '1') {
                    //    $('td', row).eq(1).html('<span class="badge badge-light-success fw-bolder px-4 py-3">Active</span>');
                    //}
                    //else {
                    //    $('td', row).eq(1).html('<span class="badge badge-light-danger fw-bolder px-4 py-3">Expired</span>');
                    //}

                    $('td', row).eq(4).addClass('text-end');
                    $('td', row).eq(4).html(
                        `<a href="${data[7]}" class="btn btn-sm btn-light btn-active-light-primary">
                            <span class="fw-bolder">Details</span>
                        </a>`);

                    //$('td', row).eq(4).html(
                    //    `<button class="btn btn-sm btn-light btn-active-light-primary" onclick="openSh('dv_sh_diag', '${data[7]}')">
                    //        <span class="fw-bolder">Details</span>
                    //    </button>`);
                }
            });

            var r = document.getElementById("tb_search");

            r && r.addEventListener("keyup", (function (t) {
                e.search(t.target.value).draw();
            }));

            var cb_verified = $('#cb_verified');
            var cb_subscribed = $('#cb_subscribed');

            var verified = '-';
            var subscribed = '-';

            $.fn.dataTable.ext.search.push(
                function (settings, data, dataIndex) {
                    var v = data[0];
                    var s = data[1];

                    if (
                        (verified === '-' && subscribed === '-') ||
                        (verified === v && subscribed === s) ||
                        (verified !== '-' && subscribed === '-' && verified === v) ||
                        (subscribed !== '-' && verified === '-' && subscribed === s)
                    ) {
                        return true;
                    }
                    return false;
                }
            );

            cb_verified.on("change", function () {
                verified = this.value;
                e.draw();
            });

            cb_subscribed.on("change", function () {
                subscribed = this.value;
                e.draw();
            });

        }()
    }
};

KTUtil.onDOMContentLoaded((function () {
    page.init()
}));

var openSh = function (modal, url) {

    var c = $(this);

    c.attr("data-kt-indicator", "on");

    $.ajax({
        type: "GET",
        url: url,
        cache: false,
        success: function (json) {
            console.log(json);
            openFullModal(modal);
        },
        error: function (error) {
            toastr.error(error.responseText);
        },
        complete: function () {
            c.attr("data-kt-indicator", "off");
        }
    });

}

//const app = Vue.createApp({
//    template: document.getElementById("appTemplate").innerHTML
//})

//app.component('my-component', {
//    template: document.getElementById("componentTemplate").innerHTML,
//    props: { name: { default: "🤷‍♂️" } }
//})

//app.mount('#app')