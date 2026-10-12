# PactNet.Extensions.Avro

A thin wrapper for Avro binary HTTP bodies using the
[hominux Avro plugin](https://github.com/hominux/pact-avro-plugin).

`WithAvroInteractions` loads the plugin. `WithAvroBody` configures a request or
response from an `.avsc` path, record name, and dictionary of field matchers.
PactNet owns the HTTP server, verification, logging, pact writing and disposal.

This development package requires the local public HTTP plugin API prototype;
the API is not in PactNet #551 or a published PactNet package. It cannot be
released until that prerequisite is contributed and published. Asynchronous
message interactions are not exposed by this wrapper.

See the repository's Avro sample and HTTP plugin contribution guide for setup.