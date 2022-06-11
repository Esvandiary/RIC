# RIC Protocol

The key words “MUST”, “MUST NOT”, “REQUIRED”, “SHALL”, “SHALL NOT”, “SHOULD”, “SHOULD NOT”, “RECOMMENDED”, “MAY”, and “OPTIONAL” in this document are to be interpreted as described in [RFC 2119](https://datatracker.ietf.org/doc/html/rfc2119).

RIC clients and servers ("endpoints") communicate via WebSocket - specifically the version standardised in [RFC 6455](https://datatracker.ietf.org/doc/html/rfc6455).  
Servers MUST provide an encrypted (`wss://`) WebSocket endpoint for clients to connect to, and MUST NOT provide an unencrypted (`ws://`) endpoint except for software testing purposes. Commensurately, clients MUST NOT support connecting to unencrypted servers except for software testing purposes.  
Servers SHOULD support the per-message compression extension to WebSocket as defined in [RFC 7692](https://datatracker.ietf.org/doc/html/rfc7692).


## WebSocket subprotocols

RIC allows for communicating via different encoding formats; these are negotiated on initial connection via WebSocket subprotocols. RIC clients MUST specify at least one subprotocol when connecting to any RIC server, and they SHOULD specify all the subprotocols they support.

* `json`: JSON-encoded data as specified in [RFC 7159](https://datatracker.ietf.org/doc/html/rfc7159)
  * All messages in this format MUST be sent as a text type WebSocket message.
  * All messages in this format MUST be encoded in UTF-8 without a byte order mark (BOM).
  * All messages in this format MUST be valid JSON, and the root JSON element SHALL be an object (dictionary).
  * RIC servers MUST support this subprotocol.
* `bson`: BSON-encoded data as specified in the [BSON v1.1 spec](https://bsonspec.org/spec.html).
  * All messages in this format MUST be sent as a binary type WebSocket message.
  * All messages in this format MUST decode to valid JSON, and the root element SHALL be an object (dictionary).
  * RIC servers SHOULD support this subprotocol, and SHOULD prefer it where supported.


## Message format

All RIC messages MUST contain the following keys in the root object:

* `time` (string): the time, according to the sender's clock, the message was sent
  * This value MUST be a valid [RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339) date using the `date-time` format (typically generated [ISO 8601](https://en.wikipedia.org/wiki/ISO_8601) datetimes will conform).
  * The sender SHOULD include fractional seconds in this value.
  * The sender SHOULD generate this value as UTC (time zone of `Z`).
  * The receiver MUST NOT infer the sender's current time zone based on the time zone of the message.
  * The sender SHOULD measure this value as late as possible during the message's generation.
  * The sender SHOULD NOT generate multiple messages with identical timestamps to send to the same receiver.
* `type` (string): the type of message being transmitted - a standalone message or part of a request/reply conversation.
  * This must be one of the values `message`, `request` or `reply`
* `name` (string): the name of the message being transmitted.
  * This value MUST be one of the message types defined later in this document (TODO).
* `data` (object): message-specific data, the validity of which is detailed alongside the relevant message.
  * This value MAY be an empty object, for example if there is no data associated with the message. It MUST NOT be null.

If the message type is `request` or `reply`, the following key MUST also be present in the root object:

* `conversation` (integer): a conversation ID tracking a request/reply combination
  * Conversations originating from the client MUST use even IDs; conversations originating from the server MUST use odd IDs
  * Values MUST fall within the range of a 32-bit unsigned integer (0..4294967295)
  * Endpoints SHOULD generate values in a manner that would not result in reuse of an ID within a timeframe that might allow for collisions
  * Endpoints MUST NOT send messages using an ID used in a previous conversation which has not yet completed

If the message type is `reply`, the following key MUST also be present in the root object:

* `status` (string): the result of the request this message is replying to
  * The possible values are message-specific, but the following values are standard:
    * A successful request MUST result in a status value of `success`
    * An invalid message (i.e. not conforming to the schema) MUST result in a status value of `invalid_message`
    * An unexpected error which cannot be categorised MUST result in a status value of `unknown_error`
  * If the value is not `success`, then the `data` property is likely to contain different data; this is detailed in individual messages


## WebSocket Paths

RIC servers may expose several different WebSocket paths to perform different operations.
The messages valid on each of the paths is documented in their own documents

* [`/ric0_chat`](Chat.md): the main path clients connect to in order to chat on the server
* [`/ric0_home`](Home.md): the path clients connect to in order to use (or register with) a home server


## Common Structures

### SoftwareVersion (object)

* `type` (string, required): the release status of the software
  * Must be one of `release`, `beta`, `alpha`, `dev`
* `major` (integer, required): the major version of the software
* `minor` (integer, required): the minor version of the piece of software
* `patch` (integer, optional, default 0): the patch version of the piece of software
* `vcs_id` (string, optional): the VCS identifier (e.g. git hash) of the software
* `display` (string, optional): the string to use for display of the version


### SupportedExtension (object)

* `name` (string, required): the name of the extension
* `versions` (array[integer], required): the supported versions of the extension


### PublicKey (object)

* `key` (string, required): the base64-encoded key data
* `format` (string, required): the format of the key data
  * This value MUST be `rsa-sha256-oaepsha256-pkcs1`
  * The format of this field is defined in [the encryption readme](Encryption.md#public-key-encryption)


### Password (object)

* `data` (string, required): the password data
* `format` (string, required): the format the password is provided in
  * Valid formats:
    * `plaintext`: the `data` property contains a plaintext password
    * `rsa-base64`: the `data` property contains a password encrypted with the receiver's public key and base64-encoded
  * Endpoints MUST support both formats, and MUST prefer `rsa-base64` where possible
  * More details are available in [the encryption readme](Encryption.md#password-transit)


### ClientAppInfo (object)

* `name` (string, required): the name of the client software
  * The name MUST remain stable across all versions of given client software
  * The name SHOULD NOT include any version information
* `description` (string, optional): the display name of the client software
* `version` (SoftwareVersion, required): the version of the client software
* `capabilities` (array[string], required): the set of optional capabilities supported by the client
* `extensions` (map{string:array[integer]}, required): the set of protocol extensions supported by the client
  * The keys are the name of the extension, and the values are the integer versions supported


### ServerAppInfo (object)

* `name` (string, required): the name of the server software
  * The name MUST remain stable across all versions of given server software
  * The name SHOULD NOT include any version information
* `description` (string, optional): the display name of the server software
* `version` (SoftwareVersion, required): the version of the server software
* `capabilities` (array[string], required): the negotiated capabilities to allow use of
  * Note that this may not be the full set of capabilities known to the server, only those also known to the client
* `extensions` (map{string:array[integer]}, required): the negotiated protocol extensions to be used
  * This value is a map, with the keys being the name of the extension and the values being the integer version to use
  * Note that this may not be the full set of extensions known to the server, only those also available to the client


### UserIdentity (object)

* `name` (string, required): the nickname of the user
* `type` (string, required): the type of the user
  * This must be one of the values: `user`, `bot`, `log_relay`
* `pubkey` (PublicKey, required): the public key of the user
* `home_server` (PublicKey, required): the public key of the user's home server
* `home_server_url` (string, optional): the canonical URL of the user's home server


### ServerIdentity (object)

* `pubkey` (PublicKey, required): the public key of the server
* `url` (string, required): the canonical URL of the server
* `name` (string, required): the name of the server
* `description` (string, optional): a description of the server