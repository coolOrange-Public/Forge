<?php

class AccessToken implements JsonSerializable
{
    private $accessToken;
    private $expiration_time;
    private $expires_in;
    private $tokenType;

    function __construct($options = [])
    {
        if (empty($options['access_token']))
            throw new InvalidArgumentException('Required option not passed: "access_token"');
        $this->accessToken = $options['access_token'];

        if (empty($options['token_type']))
            throw new InvalidArgumentException('Required option not passed: "token_type"');
        $this->tokenType = $options['token_type'];

        if (empty($options['expires_in']))
            throw new InvalidArgumentException('Required option not passed: "expires_in"');
        $this->expires_in = $options['expires_in'];

        if (!empty($options['expiration_time'])) {
            $this->expiration_time = $options['expiration_time'];
            $this->expires_in = $this->expiration_time - time();
        } else
            $this->expiration_time = time() + ((int)$this->expires_in);

    }

    function getToken()
    {
        return $this->accessToken;
    }

    function getType()
    {
        return $this->tokenType;
    }

    function getExpiresIn()
    {
        return $this->expires_in;
    }

    function getExpirationTime()
    {
        return $this->expiration_time;
    }
    public function hasExpired()
    {
        $expires = $this->expiration_time;
        if (empty($expires)) {
            throw new RuntimeException('"expires" is not set on the token');
        }
        return $expires < time();
    }

    public function __toString()
    {
        return (string)$this->getToken();
    }

    public function jsonSerialize()
    {
        return $array = [
            "token_type" => $this->getType(),
            "access_token" => $this->getToken(),
            "expires_in" => $this->getExpiresIn(),
            "expiration_time" => $this->getExpirationTime(),
        ];
    }
}