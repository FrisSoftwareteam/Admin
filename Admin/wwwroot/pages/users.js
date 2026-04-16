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

                    $('td', row).eq(4).addClass('text-end');
                    $('td', row).eq(4).html(
                        `<a class="btn btn-sm btn-light btn-active-light-primary a_reset" data-id="${data[4]}" 
                            data-name="${data[0]}" data-email="${data[1]}">
                            <span class="fw-bolder">Reset</span>
                        </a>`);
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
                    var v = data[3];

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