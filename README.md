# GHVS
Seamless navigation between Git, GitHub and Visual Studio

```
Usage: ghvs [options] [command]

Options:
  --help          Show help information
  --host          The host URL
  --access-token  The access token to use
  --secret-store  The secret store to use (Git or GHfVS)

Commands:
  branch          Show information about the current branch
  install         Install 'x-github-client' protocol handler
  issues          Show issues
  login           Login using GitHub Credential Manager
  logout          Logout using GitHub Credential Manager
  open            Open a file or folder in Visual Studio
  open-url        Open a GitHub URL in Visual Studio
  orgs            Show visible organizations (requires 'read:org' scope)
  pulls           Show pull requests
  repos           List repositories
  uninstall       Uninstall 'x-github-client' protocol handler
  upstream        Show information about the upstream repository
  viewer          Show viewer information

Run 'ghvs [command] --help' for more information about a command.
```
