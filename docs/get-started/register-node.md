# Register a node with the Pull Server

In the [LCM guide][LCM] you configured a standalone node that monitors a local
configuration document. In this guide you will point that same node at the
Pull Server so it receives its desired state centrally.

## Connect the LCM to the Pull Server

Update `appsettings.json` so the LCM pulls its desired state from the Pull
Server instead of a local file. Replace `<registration-key>` with the key
created during [Pull Server setup][pull-server].

<!-- markdownlint-disable MD040 -->
```json title="appsettings.json"
{
  "LCM": {
    "PullServer": {
      "ServerUrl": "http://localhost:5000",
      "RegistrationKey": "<registration-key>",
      "CertificateSource": "Managed",
      "ReportCompliance": true
    }
  }
}
```
<!-- markdownlint-enable MD040 -->

See [LCM configuration] for the `appsettings.json` location on each
platform and a full explanation of the Pull Server settings.

## Verify node registration

Open the Pull Server admin console and navigate to the **Nodes** page. Your
machine should appear with its FQDN and registration timestamp.

Once the node is registered, you can
[create your first configuration][first-configuration] and assign it.

[LCM]: lcm.md
[LCM configuration]: lcm.md#configure
[pull-server]: pull-server.md
[first-configuration]: first-configuration.md
