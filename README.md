# GHVS
Seamless navigation between Git, GitHub and Visual Studio

## Getting started

1. Add the following alias to your dotfiles or run

```
# Bootstrap run.sh for ghvs tool
alias ghvs='source <(curl -Lks https://raw.githubusercontent.com/jcansdale/ghvs/master/run.sh)'
```

2. Clone a repository

```
git clone https://github.com/jcansdale/ghvs
cd ghvs
```

3. Pass a GitHub URL to `ghvs`

The following GitHub URLs are supported.


```
# An added or changed line from the diff view.
ghvs https://github.com/jcansdale/ghvs/pull/35/files#diff-1b0c2b516b83393edb7200ad5ff12181R8

# Any file in Git repo
ghvs https://github.com/jcansdale/ghvs/blob/c424c015135f89d5e9a00f40df67f88bee73dd5b/run.sh#L8

# An inline comment
ghvs https://github.com/jcansdale/ghvs/pull/35#pullrequestreview-418359438
```

## Usage

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
