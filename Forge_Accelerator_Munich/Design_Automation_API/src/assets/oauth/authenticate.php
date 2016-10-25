<?php
require_once __DIR__ . '/src/Authentication.php';
require_once __DIR__ . '/src/AccessToken.php';

session_start();

function authenticate($scope)
{
    $authentication = new Authentication($scope);
    $accessToken = $authentication->getAccessToken();
    return json_encode($accessToken);
}

function getScope()
{
    if (!isset($_GET['scope']))
        return 'data:read';
    if (!is_array($_GET['scope']))
        return $_GET['scope'];
    sort($_GET['scope']);
    return implode(" ", $_GET['scope']);
}

$scope = getScope();
if (!isset($_SESSION[$scope])) {
    $_SESSION[$scope] = authenticate($scope);
} else {
    $options = json_decode($_SESSION[$scope], true);
    $accessToken = new AccessToken($options);
    if ($accessToken->hasExpired())
        $_SESSION[$scope] = authenticate($scope);
    $_SESSION[$scope] = json_encode($accessToken);
}

//Jsonp callback
if (isset($_GET['callback']))
    echo $_GET['callback'] . '(' . $_SESSION[$scope] . ')';
else
    echo $_SESSION[$scope];