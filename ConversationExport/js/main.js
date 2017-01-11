// Scripts to pull down the convos if they are less, and scroll down if they are a lot of them

$(window).load(function() {
    $("html, body").animate({ scrollTop: $(document).height() }, 1000);
    $('.pull-down').each(function() {
        var $this = $(this);
        var x = $this.parent().height() - $this.height()
        if(x > 0) {
            $this.css('margin-top', x)
        }
    });
});


//Exported bubbles go here
angular.module("app", []).controller('BubbleController', ['$scope', 'exportedBubbles', function ($scope, exportedBubbles) {
    $scope.bubbles = exportedBubbles.bubbles.reverse();

    $scope.formatDate = function (timestamp) {
        var days = ["Sunday","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"];
        var months = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
        var date = new Date(timestamp*1000);
        var hours = date.getHours();
        var minutes = "0" + date.getMinutes();
        var seconds = "0" + date.getSeconds();
        var day  = date.getDay();

        var formattedTime = days[day] + " "  + (parseInt(date.getDay())+1) + " " + months[date.getMonth()] + " " + date.getFullYear() + " at " + hours + ':' + minutes.substr(-2) + ':' + seconds.substr(-2);
        return formattedTime;
    };

    $scope.getPath = function(path) {
        var filename = path.replace(/^.*[\\\/]/, '');
        return "media/" + filename;
    };
}]);

angular.module("app").filter("trustUrl", ['$sce', function ($sce) {
    return function (recordingUrl) {
        return $sce.trustAsResourceUrl(recordingUrl);
    };
}]);