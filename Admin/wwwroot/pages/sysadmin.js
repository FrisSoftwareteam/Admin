let userid = 0;
let name = '';
let current;

$('.a_roles').click(function () {
    current = this;

    var array = current.getAttribute('data-roles').split(',');
    var checkboxes = document.querySelectorAll('input.chk_role');
    for (var checkbox of checkboxes) {
        checkbox.checked = array.indexOf(checkbox.getAttribute('data-role')) >= 0;
    }

    userid = current.getAttribute('data-id');
    name = current.getAttribute('data-name');

    $('#sp_name').html(name);
});

$('.chk_role').change(function () {

    $.ajax({
        type: "POST",
        url: $('#a_switch_url').attr('href'),
        data: {
            id: userid,
            role: this.getAttribute('data-role'),
            status: this.checked
        },
        cache: false,
        success: function (json) {
            current.setAttribute('data-roles', json.roles);
            toastr.success(`Update was successful for ${name}`);
        },
        error: function () {
            toastr.error('could not update status');
        }
    });

});