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
                    $(row).attr('id', `tr_${data[4]}`);

                    $('td', row).eq(5).html(
                        `<span id='b_${data[5]}' class="badge badge-${data[7]}">${data[5]}</span>`
                    );

                    $('td', row).eq(6).addClass('text-end');
                    $('td', row).eq(6).html(
                        `<a class="btn btn-sm btn-light btn-active-light-primary" href="${data[8]}" 
                            data-id="${data[6]}" data-code="${data[0]}" data-name="${data[2]}">
                            <span class="fw-bolder">Details</span>
                        </a>`
                    );
                }
            });

            var r = document.getElementById("tb_search");

            r && r.addEventListener("keyup", (function (t) {
                e.search(t.target.value).draw();
            }));

            var cb_type = $('#cb_type');

            var type = '-';

            $.fn.dataTable.ext.search.push(
                function (settings, data, dataIndex) {
                    var v = data[1];

                    if (type === '-' || type === v) {
                        return true;
                    }
                    return false;
                }
            );

            cb_type.on("change", function () {
                type = this.value;
                e.draw();
            });

        }()
    }
};

KTUtil.onDOMContentLoaded((function () {
    page.init()
}));