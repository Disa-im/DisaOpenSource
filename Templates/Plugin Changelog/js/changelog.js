// Get the paramters from the URL (i.e. alpha status and version number)
$.urlParam = function(name, url) {
    if (!url) {
				url = window.location.href;
    }
    var results = new RegExp('[\\?&]' + name + '=([^&#]*)').exec(url);
    if (!results) { 
        return undefined;
    }
    return results[1] || undefined;
}

$(document).ready(function(){
		// Hide all version-changes elements on page load.
		$('.version-changes').hide();
		
		show_on_click();

		var hidenav = $.urlParam("hidenav");
		if (hidenav == 'true') {
				$('.disa-page-header').hide();
		}
    
		// If there is an "alpha" parameter...
    var is_alpha = $.urlParam("alpha");
		// If the parameter is false hide the changelogs tagged "alpha". 
		if(is_alpha != 'true') {
				$('.alpha').hide();
		}
    
    var version_number = $.urlParam("version");
		if(version_number == 'new')
		{
				$('html, body').animate({
						scrollTop: ($('.container').children('.version').first().offset().top - 75)
				}, 1500);
				$('.container').children('.version').first().closest('div').find('.version-changes').slideToggle();
		} else {
				$('html, body').animate({
						scrollTop: ($('.' + version_number).offset().top - 75)
				}, 1500);
				$('.' + version_number).closest('div').find('.version-changes').slideToggle();
		}
});

// Open/close the details on a changelog when it is clicked
function show_on_click(){	
		$('.version').bind('click', function(){
				$('.version').not(this).closest('div').find('.version-changes').slideUp();
				$(this).closest('div').find('.version-changes').slideToggle();
		});
}