const corporate = 'corporate';
const options = document.querySelectorAll('[data-option="application-type"]');

options.forEach(option => {
    option.addEventListener('click', e => {
        e.preventDefault();

        const optionValue = e.target.getAttribute('data-option-value');

        document.querySelectorAll('[data-option-view]').forEach(function (element) {
            element.classList.add('d-none');
        });

        //console.log(`[data-option-view="${optionValue}-only"]`);

        document.querySelectorAll(`[data-option-view="${optionValue}-only"]`).forEach(function (element) {
            element.classList.remove('d-none');
        });
    });
});