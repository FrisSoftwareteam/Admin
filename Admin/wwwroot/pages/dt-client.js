"use strict";
var SH_Dash = {
	init: function () {
		! function () {
			const t = document.getElementById("tb_data");
			if (!t) return;
			const e = $(t).DataTable({
				info: !1,
				order: [],
				columnDefs: [{
					targets: 0,
					orderable: !1
				}]
			});
			var r = document.getElementById("tb_data_search");
			r && r.addEventListener("keyup", (function (t) {
				e.search(t.target.value).draw()
			}))
		}()
	}
};
KTUtil.onDOMContentLoaded((function () {
	SH_Dash.init()
}));