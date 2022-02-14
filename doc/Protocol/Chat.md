# Messages: `/ric0_chat`

All messages on this path require the user to have authenticated, unless otherwise specified.

Until authentication is completed (via a `challenge` request followed by a successful `connect` request),
all messages requiring authentication MUST be ignored and servers SHOULD immediately close the connection if they receive one.



### `challenge` (request, reply)

This message gets the server's public key and verifies its identity. This request does not require the user to be authenticated.

If the client cannot verify that the challenge was signed by the private key matching the server's public key, it MUST immediately close the connection.

Request data:

* `challenge` (string, required): the base64-encoded string to verify with

Possible response status values are: `success`, `invalid_challenge`

Response data:

* `pubkey` (PublicKey, required): the server's public key
* `challenge_response` (string, required): the challenge string, hashed and signed with the server's private key and base64-encoded



### `connect` (request, reply)

This message allows the server to verify the client's identity and negotiate their respective capabilities. This request does not require the user to have authenticated.

`connect` request data:

* `client_app` (ClientAppInfo, required): information about the client software
* `user` (UserIdentity, required): information about the user connecting
* `challenge` (string, required): the server's public key bytes, hashed and signed with the user's private key, base64-encoded
* `join_token` (string, optional): a join token to connect to this server


Possible response status values are: `success`, `already_connected`, `invalid_challenge`, `unrecognised_user`, `disallowed_client`, `disallowed_user`, `invalid_join_token`, `join_token_required`

Response data for successful `connect` requests:

* `server_app` (ServerAppInfo, required): information about the server software
* `server_identity` (ServerInfo, required): information about the server's identity
* (TODO: server-specific user profile?)

Response data for failed `connect` requests:

* `disallowed_client_reason` (string, optional): the reason a client is disallowed by the server
  * Note that this value is optional even when the failure reason is a disallowed client
* `disallowed_user_reason` (string, optional): the reason a user is disallowed by the server
  * Note that this value is optional even when the failure reason is a disallowed user



### `disconnect` (request, reply)

This message disconnects the current user from the chat server.

`disconnect` request data:

* `reason` (string, optional): the reason the user is disconnecting

Possible response status values are: `success`, `not_connected`

Response data is empty.