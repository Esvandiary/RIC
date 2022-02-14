# Messages: `/ric0_home`

All messages on this path require the user to have authenticated, unless otherwise specified.

Until authentication is completed (via a successful `login` request), all messages requiring authentication MUST be ignored
and servers SHOULD immediately close the connection if they receive one.



### `challenge` (request, reply)

This message gets the server's public key and verifies its identity. This request does not require the user to be authenticated.

If the client cannot verify that the challenge was signed by the private key matching the server's public key, it MUST immediately close the connection.

Request data:

* `challenge` (string, required): the base64-encoded string to verify with

Response data:

* `pubkey` (PublicKey, required): the server's public key
* `challenge_response` (string, required): the challenge string, hashed and signed with the server's private key and base64-encoded



### `register` (request, reply)

This registers a new user on a home server. This request does not require the user to have authenticated.

Request data:

* `username` (string, required): the username to register with
* `password` (Password, required): the password to register with
* `join_token` (string, optional): a join token to register on this server

Possible response status values are: `success`, `registration_closed`, `invalid_username`, `username_in_use`, `invalid_password`, `invalid_password_format`, `invalid_join_token`, `join_token_required`

Response data is empty for successful requests.

Response data for failed requests:

* `username_error_reason` (string, optional): description of the reason a username was invalid
* `password_error_reason` (string, optional): description of the reason a password was invalid



### `login` (request, reply)

This message allows servers and clients to negotiate their respective capabilities and agree on a common set to use, as well as informing the client what services are provided by the server.
This request does not require the user to have authenticated.

Request data:

* `client_app` (ClientAppInfo, required): information about the client software
* `username` (string, required): the username to log in with
* `password` (Password, required): the password to log in with
* `client_token` (string, optional): a client token identifying the client as previously logged in
* `mfa_token` (string, optional): a current multi-factor authentication token for the account
* `join_token` (string, optional): a join token to connect to this server

Possible response status values are: `success`, `already_logged_in`, `login_closed`, `mfa_required`, `disallowed_client`, `disallowed_user`, `unrecognised_user`, `invalid_password`, `invalid_mfa_token`, `invalid_join_token`, `join_token_required`

Response data for successful requests:

* `server_app` (ServerAppInfo, required): information about the server software
* `server_identity` (ServerIdentity, required): information about the server's identity
* `user` (UserIdentity, required): information about the user now logged in
* `client_token` (string, optional): a token to recognise previously-used clients

Response data for failed requests:

* `disallowed_client_reason` (string, optional): the reason a client is disallowed by the server
  * Note that this value is optional even when the failure reason is a disallowed client
* `disallowed_user_reason` (string, optional): the reason a user is disallowed by the server
  * Note that this value is optional even when the failure reason is a disallowed user



### `logout` (request, reply)

This message logs out the user currently associated with this connection.

Request data is empty.

Possible response status values are: `success`, `not_logged_in`

Response data is empty.



### `decrypt` (request, reply)

This message allows the current user to decrypt one or more messages with their private key as held by the home server.

Request data:

* `encrypted_messages` (array[string], required): an array of base64-encoded messages to be decrypted

Possible response status values are: `success`, `not_logged_in`, `invalid_messages`

Response data for successful requests:

* `decrypted_messages` (array[string], required): an array of base64-encoded messages decrypted with the user's private key
  * The messages are returned in the same order as in the request

Response data for failed requests:

* `invalid_messages` (array[string], required): an array of those requested messages which could not be base64-decoded or decrypted



### `sign` (request, reply)

This message allows the current user to hash and sign one or more messages with their private key as held by the home server.

Request data:

* `messages` (array[string], required): an array of base64-encoded messages to be hashed and signed
* `hash` (string, required): the hash to use

Possible response status values are: `success`, `not_logged_in`, `invalid_messages`, `invalid_hash`

Response data for successful requests:

* `signed_hashes` (array[string], required): an array of base64-encoded hashes signed with the user's private key
  * The hashes are returned in the same order as the messages in the request

Response data for failed requests:

* `invalid_messages` (array[string], required): an array of those requested messages which could not be base64-decoded or decrypted
* `supported_hashes` (array[string], required): the list of hashes the server supports