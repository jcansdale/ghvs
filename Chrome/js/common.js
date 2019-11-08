function createOpenEditorButton(pullRequestWebUrl)
{
	var openEditorButton = document.createElement("a");
	openEditorButton.setAttribute("id", "ghvs");
	openEditorButton.href = 'x-github-client://openRepo/' + pullRequestWebUrl;
	openEditorButton.setAttribute("class", "btn");
    var textNode = document.createTextNode("Open in Editor");
	openEditorButton.appendChild(textNode);
	
	return openEditorButton;
}
