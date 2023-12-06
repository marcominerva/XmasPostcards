async function generateRandomPostcard(language) {
    const response = await fetch('/api/postcard', {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Accept-Language": language
        }
    });

    return response;
}

function GetErrorMessage(statusCode, content)
{
    if (statusCode >= 200 && statusCode <= 299)
        return null;

    if (content.errors)
    {
        return `${content.title ?? content} (${content.errors[0].message})`;
    }

    return content.detail ?? content.title ?? content;
}