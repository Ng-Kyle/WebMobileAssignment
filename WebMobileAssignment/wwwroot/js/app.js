// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.target.dataset.get;
    location = url || location;
});

// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = e.target.dataset.post;
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location;
    f.submit();
});

// Trim input
$('[data-trim]').on('change', e => {
    e.target.value = e.target.value.trim();
});

// Auto uppercase
$('[data-upper]').on('input', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// RESET form
$('[type=reset]').on('click', e => {
    e.preventDefault();
    location = location;
});

// Check all checkboxes
$('[data-check]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.check;
    $(`[name=${name}]`).prop('checked', true);
});

// Uncheck all checkboxes
$('[data-uncheck]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.uncheck;
    $(`[name=${name}]`).prop('checked', false);
});

// Row checkable (AJAX-supported)
$(document).on('click', '[data-checkable]', e => {
    if ($(e.target).is(':input,a')) return;
    
    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo preview
$('.upload input').on('change', e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0];

    img.dataset.src ??= img.src;

    if (f && f.type.startsWith('image/')) {
        img.onload = e => URL.revokeObjectURL(img.src);
        img.src = URL.createObjectURL(f);
    }
    else {
        img.src = img.dataset.src;
        e.target.value = '';
    }

    // Trigger input validation
    $(e.target).valid();
});

//Append Images to Add Image container
$('.addanime_photo-label input').on('change', e => {
    const files = e.target.files;
    const container = $(e.target).closest('.addanime_input-container').find('.image-previews');
    container.empty();
    for (const f of files) {
        if (f && f.type.startsWith('image/')) {
            const img = $('<img>');
            img.attr('src', URL.createObjectURL(f));
            img.addClass('image-previews_img');
            img.onload = e => URL.revokeObjectURL(img.src);
            container.append(img);
        }
    }
    // Trigger input validation
    $(e.target).valid();
});

//Update Table on filter selection
$(document).on('change', '.filter_select' ,e => {
    $('.anime_table-form').submit();
})

//Append another add anime form inputs
$(document).on('click', '#addanotheranime', e => {
    const container = $('#addanime_container');
    const original = $('.addanime_batch').first();
    const index = $('.addanime_batch').length;

    const clone = original.clone();

    clone.attr('anime-index', index);
    clone.find('h2').text(`Anime ${index + 1}`);
    clone.find('[name]').each((i, e) => {
        e.name = e.name.replace(
            /AnimeVMs\[\d+\]/g,
            `AnimeVMs[${index}]`
        );
    });
    clone.find('[id]').each((i, e) => {
        e.id = e.id.replace(
            /AnimeVMs_\d+__/g,
            `AnimeVMs_${index}__`
        );
    });
    clone.find('[for]').each((i, e) => {
        e.htmlFor = e.htmlFor.replace(
            /AnimeVMs_\d+__/g,
            `AnimeVMs_${index}__`
        );
    });
    clone.find('[data-valmsg-for]').each((i, e) => {
        e.setAttribute(
            'data-valmsg-for',
            e.getAttribute('data-valmsg-for').replace(
                /AnimeVMs\[\d+\]/g,
                `AnimeVMs[${index}]`
            )
        );
    });
    clone.find('input[name="__Invariant"]').each((i, e) => {
        e.value = e.value.replace(
            /AnimeVMs\[\d+\]/g,
            `AnimeVMs[${index}]`
        );
    });

    clone.find('input').not('[type="hidden"], [type="checkbox"]').val('');
    clone.find('textarea').val('');
    clone.find('select').prop('selectedIndex', 0);
    clone.find('input[type="checkbox"]').prop('checked', false);
    clone.find('[name$=".Episodes"]').val(1);
    clone.find('.image-previews').empty();
    clone.find('input[type="date"]').removeAttr('max');
    container.append(clone);
});

$(document).on('change', '.addanime_batch select[name$=".Status"]', e => {
    const batch = $(e.currentTarget).closest('.addanime_batch');
    const status = $(e.currentTarget).val();
    const date = batch.find('input[type="date"]');

    if (status !== 'Upcoming') {
        const today = new Date().toISOString().split('T')[0];

        date.val(today);
        date.attr('max', today);
    } else {
        date.val('');
        date.removeAttr('max');
    }
});