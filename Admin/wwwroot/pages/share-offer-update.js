"use strict";

var page = {
    init: function () {
        ! function () {
            const chk_offer = document.getElementById("AllowPublicOffer");
            const chk_right = document.getElementById("AllowRightIssue");
            
            const div_offer = document.getElementById("div_public_offer");
            const div_right = document.getElementById("div_right_issue");
            
            chk_offer.addEventListener("change", function () {
                div_offer.classList.toggle("d-none", !this.checked);
                div_offer.classList.toggle("d-block", this.checked);
            });

            chk_right.addEventListener("change", function () {
                div_right.classList.toggle("d-none", !this.checked);
                div_right.classList.toggle("d-block", this.checked);
            });
            
        }()
    }
};

KTUtil.onDOMContentLoaded((function () {
    page.init()
}));