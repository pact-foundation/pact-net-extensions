# PactNet.Extensions.Avro

A thin wrapper for Avro binary HTTP bodies and asynchronous messages using the
[hominux Avro plugin](https://github.com/hominux/pact-avro-plugin).

`WithAvroInteractions` loads the plugin. `WithAvroBody` configures a request or
response from an `.avsc` path, record name, and dictionary of field matchers.
`WithAvroMessages` and `WithAvroContent` pass generated binary records and metadata
to the real consumer without starting a broker or transport server. PactNet owns
verification, logging, pact writing and disposal.

This development package requires the local public HTTP/async plugin API prototype;
the API is not in PactNet #551 or a published PactNet package. It cannot be
released until that prerequisite is contributed and published. Plugin 0.1.3 does
not populate distinct synchronous request/response message parts; see the messaging
guide for the required plugin and PactNet follow-up contributions.

See the repository's Avro sample and HTTP plugin contribution guide for setup.