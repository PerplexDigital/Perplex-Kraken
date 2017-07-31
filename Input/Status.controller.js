/*
 * Kraken status and option datatype.
 * This datatype optimizes individual images. If the image was already optimized it shows the relevant status.
 */

angular.module("umbraco").controller("Status.Controller", function ($scope, krakenOptionsResource, $routeParams, notificationsService) {

    // Status enum that contains all available statusses
    $scope.enmStatus = {
        Loading: 0,
        MissingCredentials: 1,
        Unkrakable: 2,
        Krakable: 3,
        Kraked: 4,
        Original: 5,
    };

    // Default status
    $scope.status = $scope.enmStatus.Loading;
    $scope.kraking = false;

    $scope.init = function () {
        $scope.getStatus();
    };

    // Returns the current status
    $scope.getStatus = function () {
        krakenOptionsResource.getStatus($routeParams.id, $scope.model.value)
            .success(function (data, status, headers, config) {
                $scope.status = data;
            }).error(function (data, status, headers, config) {
                // Do nothing?
                // Set status as not krakeble
                $scope.status = $scope.enmStatus.Unkrakable;
            });
    };

    // Start kraking
    $scope.krake = function () {
        if ($scope.kraking != true) {
            $scope.kraking = true;
            krakenOptionsResource.krake($routeParams.id)
                .success(function (data, status, headers, config) {
                    if (data && data.Success === true) {
                        // Success!
                        $scope.kraking = true;
                        // Reload the page
                        location.reload();
                    }
                    else
                    {
                        if (data.Message != null)
                            // Show the server error to the user
                            notificationsService.error(data.Message);
                        else
                            notificationsService.error("An error has occured while optimizing your image, please try again.");
                        $scope.kraking = false;
                    }
                }).error(function (data, status, headers, config) {
                    $scope.kraking = false;
                    // Show error
                    if (data && data.Message != null)
                        notificationsService.error(data.Message);
                    else
                        notificationsService.error("An error has occured while optimizing your image, please try again.");
                });
        } else {
            notificationsService.error("Optimization in progress, please wait.");
        }
    };
});

angular.module("umbraco.resources")
    .factory("krakenOptionsResource", function ($http) {
        return {
            // Returns the current media status
            getStatus: function (imageId, propVal) {
                return $http({
                    method: "GET",
                    url: "/umbraco/backoffice/kraken/krakenapi/getstatus",
                    params: { imageId: imageId, propVal: propVal },
                })
            },
            // Start the kraking process
            krake: function (imageId) {
                return $http({
                    method: "GET",
                    url: "/umbraco/backoffice/kraken/krakenapi/Optimize",
                    params: { imageId: imageId },
                })
            },
        };
    });