# Create your first configuration

Now that a node is [registered with the Pull Server][register-node], you can
create a configuration document, publish it, and assign it to the node.

In [Using configuration documents with DSC CLI][config-docs] you learned how to
apply a configuration locally. Let's reuse that example to manage a directory
through the Pull Server instead.

Create a file called `main.dsc.yaml` with the same content:

<!-- markdownlint-disable MD046 -->
<!-- markdownlint-disable MD040 -->

=== ":fontawesome-brands-windows: Windows"

    ```yaml title="main.dsc.yaml"
    $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
    resources:
      - name: Create demo directory
        type: OpenDsc.FileSystem/Directory
        properties:
          path: C:\temp\demo
          exist: true
    ```

=== ":fontawesome-brands-linux: Linux"

    ```yaml title="main.dsc.yaml"
    $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
    resources:
      - name: Create demo directory
        type: OpenDsc.FileSystem/Directory
        properties:
          path: /tmp/demo
          exist: true
    ```

<!-- markdownlint-enable MD040 -->
<!-- markdownlint-enable MD046 -->

## Upload the configuration document

Unlike the DSC CLI where you apply a configuration directly from your machine,
the Pull Server needs a copy of the document so it can distribute it to every
assigned node. Upload the file through the admin console:

1. Navigate to **Configurations**.
2. Click **Create**.
3. Enter the name `LabConfig`.
4. Set the entry point to `main.dsc.yaml`.
5. Upload the `main.dsc.yaml` file you just created.
6. Click **Save**.

![Upload configuration][upload-configuration]

## Publish the configuration document

The configuration is created in `Draft` status. Publish it to make it available
to nodes. For more on versioning and lifecycle, see
[configuration management][configuration-management].

1. On the **Configurations** page, click on `LabConfig`.
2. Click **Publish** next to version `1.0.0`.

![Publish configuration version][publish-configuration-version]

## Assign the configuration to a node

To distribute the configuration to a node, assign it from the admin console.
The LCM on the node will pick it up on its next check-in and apply the desired
state.

1. Navigate to **Nodes**.
2. Click on your registered node.
3. Under **Configuration**, select `LabConfig`.
4. Click **Save**.

![Assign configuration to node][assign-configuration]

The LCM will apply the configuration on its next evaluation cycle based on the
`ConfigurationModeInterval` set in `appsettings.json`. If you don't want to
wait, restart the LCM service to trigger an immediate check-in.

[register-node]: register-node.md
[config-docs]: using-configuration-documents-with-dsc-cli.md
[configuration-management]: ../concepts/pull-server/configuration-management.md

[upload-configuration]: media/pull-server-setup/upload-configuration.png
[publish-configuration-version]: media/pull-server-setup/publish-version.png
[assign-configuration]: media/pull-server-setup/assign-configuration.png
