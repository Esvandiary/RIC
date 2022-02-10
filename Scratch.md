
User model
  - per-server with home server to hold keys?
    - ~solves the pubkey issue, user/pass to log into your home server which has the keys
    - ~solves the "private home server" scenario
    - send the keys to the client once user/pass (and 2FA if enabled) is verified?
      - would mean home server is irrelevant other than keyholder and knowledge of persistent client state
      - MITM concerns? no way we should allow ws:// if we do this
    - client just makes requests to home server for signing? would mean no keys across the wire
      - could be slow? only once per connect to each server though, and could batch them on client start
    - allows for cross-server identity verification
