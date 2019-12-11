function createOpenEditorButton(pullRequestWebUrl)
{
	var openEditorButton = document.createElement("a");
	openEditorButton.setAttribute("id", "ghvs");
	openEditorButton.href = 'x-github-client://openRepo/' + pullRequestWebUrl;
	openEditorButton.setAttribute("class", "btn");
    var textNode = document.createTextNode("Open editor");
	openEditorButton.appendChild(textNode);
	
	return openEditorButton;
}
