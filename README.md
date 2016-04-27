# Render PDF SharePoint handler

## Installation instructions

 1. Build the WSP file
 2. Deploy the WSP

## Usage
Open in browser: 
http://sharepoint.url/sites/site/_layouts/15/nova.sp.pdf.helper/GeneratePdf.ashx?url=http%3A%2F%2Fgoogle.com%2F%3Fq%3Dnova

 - the request type must be "GET"
 - the url query string parameter is mandatory
 - the url value should be encoded using JavaScript encodeURIComponent or .net Uri.EscapeDataString

## How it works
The [PhantomJs](http://phantomjs.org/) is used to render the URL from the query string. The executabled is include in the project in the manifest and is installed into web application (IIS) bin folder. The standard PhantomJs _rasterize.js_ script is used to render the page. The script and config file for PhantomJs are included in project as embedded resources.

The page is implemented as HTTP handler. It returns the rendered PDF or error info as HTML.

## Remarks
The project was created for SP2013 using VS2015, but it can be "downgraded" to SharePoint 2010. Some code changes are required, as it uses few of new .net 4.0 features, i.e.: template strings.
