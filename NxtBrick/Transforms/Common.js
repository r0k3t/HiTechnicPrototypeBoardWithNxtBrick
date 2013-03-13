//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: Common.js $ $Revision: 3 $
//-----------------------------------------------------------------------

var xmlUpdateState = null;
var stateComplete;
var counter = 

function createXmlHttpRequest()
{
    if (window.XMLHttpRequest)
    {
        return new XMLHttpRequest();
    }
    else if (window.ActiveXObject)
    {
        return new ActiveXObject("Microsoft.XMLHTTP");
    }
    else
    {
        return null;
    }
}

function updateState()
{
  if (xmlUpdateState == null)
  {
    xmlUpdateState = createXmlHttpRequest()
    if (xmlUpdateState == null)
    {
        return;
    }
    
    xmlUpdateState.open("GET", self.location.href, true);
    xmlUpdateState.onreadystatechange = handleUpdateState;
    xmlUpdateState.setRequestHeader("If-Modified-Since", new Date(0));
    xmlUpdateState.send();
  }
}

function readState(interval)
{
    setTimeout("updateState()", interval);
}

function setStateCompletion(completion)
{
    stateComplete = new Function("state", completion + "(state);");
}

function handleUpdateState()
{
  if (xmlUpdateState.readyState == 4)
  {
    try
    {
      var response = xmlUpdateState.responseXML;
      var state = objectify(response.documentElement);
      
      stateComplete(state);
    }
    catch(expression)
    {
    }
    
    xmlUpdateState = null;
  }
}

function objectify(xmlElement)
{
  var children = xmlElement.childNodes;
  var index;
  var obj;

  for (index = 0; index < children.length; index++)
  {
    var child = children.item(index);
    if (child.nodeType == 3) // text
    {
      obj = child.nodeValue;
    }
    else if (child.nodeType == 1) // element
    {
      if (obj == undefined)
      {
        obj = new Object();
      }
      obj[child.baseName] = objectify(child);
    }
  }

  return obj;
}


