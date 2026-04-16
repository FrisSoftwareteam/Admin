"use strict";

var page = {
    init: function () {
        ! function () {
            const t = document.getElementById("tb_data");

            if (!t) return;

            const e = $(t).DataTable({
                info: 1,
                order: [0],
                ajax: t.getAttribute('data-url'),
                columnDefs: [{
                    targets: 2,
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
                    $(row).attr('id', data[2]);

                    if (data[0] == '1') {
                        $('td', row).eq(0).html(
                            `<div class="d-flex align-items-center me-2">
                                <span class="svg-icon svg-icon-3tx svg-icon-success me-4">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none">
                                        <path opacity="0.3" d="M10.3 14.3L11 13.6L7.70002 10.3C7.30002 9.9 6.7 9.9 6.3 10.3C5.9 10.7 5.9 11.3 6.3 11.7L10.3 15.7C9.9 15.3 9.9 14.7 10.3 14.3Z" fill="black" />
                                        <path d="M22 12C22 17.5 17.5 22 12 22C6.5 22 2 17.5 2 12C2 6.5 6.5 2 12 2C17.5 2 22 6.5 22 12ZM11.7 15.7L17.7 9.70001C18.1 9.30001 18.1 8.69999 17.7 8.29999C17.3 7.89999 16.7 7.89999 16.3 8.29999L11 13.6L7.70001 10.3C7.30001 9.89999 6.69999 9.89999 6.29999 10.3C5.89999 10.7 5.89999 11.3 6.29999 11.7L10.3 15.7C10.5 15.9 10.8 16 11 16C11.2 16 11.5 15.9 11.7 15.7Z" fill="black" />
                                    </svg>
                                </span>
                                <div>
                                    <div class="d-flex flex-column justify-content-center">
                                        <a href="/shareholders/details/${data[3]}"
                                            class="mb-1 text-gray-800 text-hover-primary h3">${data[4]}</a>
                                        <div class="fw-bold fs-6 text-gray-400">${data[5]}</div>
                                    </div>
                                </div>
                            </div>`);
                    }
                    else {
                        $('td', row).eq(0).html(
                            `<div class="d-flex align-items-center me-2">
                                <span class="svg-icon svg-icon-3tx svg-icon-warning me-4">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="24px" height="24px" viewBox="0 0 24 24" version="1.1">
                                        <circle fill="#000000" opacity="0.3" cx="12" cy="12" r="10"></circle>
                                        <rect fill="#000000" x="11" y="7" width="2" height="8" rx="1"></rect>
                                        <rect fill="#000000" x="11" y="16" width="2" height="2" rx="1"></rect>
                                    </svg>
                                </span>
                                <div>
                                    <div class="d-flex flex-column justify-content-center">
                                        <a href="/shareholders/details/${data[3]}"
                                            class="mb-1 text-gray-800 text-hover-primary h3">${data[4]}</a>
                                        <div class="fw-bold fs-6 text-gray-400">${data[5]}</div>
                                    </div>
                                </div>
                            </div>`);
                    }


                    if (data[1] == '1') {
                        $('td', row).eq(1).html('<span class="badge badge-light-success fw-bolder px-4 py-3">Active</span>');
                    }
                    else {
                        $('td', row).eq(1).html('<span class="badge badge-light-danger fw-bolder px-4 py-3">Expired</span>');
                    }

                    $('td', row).eq(2).addClass('text-end');
                    $('td', row).eq(2).html(
                        `<a href="/shareholders/details/${data[3]}" class="btn btn-sm btn-light btn-active-light-primary">
                            <span class="fw-bolder">Details</span>
                        </a>`);
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