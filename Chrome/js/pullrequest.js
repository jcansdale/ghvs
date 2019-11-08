// Check if the GHVS buttons already exists, return if this is the case.
var ghvsButton = document.getElementById("ghvs");
if (ghvsButton == null)
{
	var containers = document.querySelectorAll("div.js-resolvable-timeline-thread-container");
	containers.forEach(function (container)
	{
		var commentLink = container.querySelectorAll("a.js-timestamp.d-inline-block")[0];
		var openEditorButton = createOpenEditorButton(commentLink.href);
		var threadForm = container.querySelectorAll("form.js-resolvable-timeline-thread-form")[0];
		threadForm.appendChild(openEditorButton);
	});
}
