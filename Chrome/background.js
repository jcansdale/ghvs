chrome.webNavigation.onHistoryStateUpdated.addListener(executeScripts);
chrome.webNavigation.onCompleted.addListener(executeScripts);
chrome.webNavigation.onTabReplaced.addListener(executeScripts);

function executeScripts (details)
{
	// Load common js scripts.
	chrome.tabs.executeScript(null,{file:"js/common.js"});
	// Get the URL without the parameters.
	var url = details.url;
	url = url.split("?")[0];

	if (url.indexOf("/pull/") > -1)
	{
		// Specific pull request page, load pullrequest.js.
		chrome.tabs.executeScript(null,{file:"js/pullrequest.js"});
	}
	// else: this is not a pull request web page, ignore.
}