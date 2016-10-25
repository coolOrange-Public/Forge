<?php
require __DIR__ . '/../vendor/autoload.php';
require __DIR__ . '/AccessToken.php';

class Authentication
{
    private $clientId = 'Vc3eFKAIBZfkqu50TfsDqYQrlKbZqMmo';
    private $clientSecret = 'VAiGKBGzaehGZlfC';
    private $authenticateUrl = 'https://developer.api.autodesk.com/authentication/v1/authenticate';

    private $accessToken;
    private $scope;


    function __construct($scope)
    {
        $this->scope = $scope;
        $this->accessToken = $this->generateToken();
    }

    public function getUrl()
    {
        return $this->authenticateUrl;
    }

    public function getScope()
    {
        return $this->scope;
    }

    public function getAccessToken()
    {
        return $this->accessToken;
    }

    private function generateToken()
    {
        $client = new \GuzzleHttp\Client();
        $response = $client->post($this->authenticateUrl, [
            'form_params' => [
                'client_id' => $this->clientId,
                'client_secret' => $this->clientSecret,
                'grant_type' => 'client_credentials',
                'scope' => $this->scope
            ]
        ]);

        $options = json_decode($response->getBody(), true);
        return new AccessToken($options);
    }
}

