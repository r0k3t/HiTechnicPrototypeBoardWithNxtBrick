<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Brick.user.xslt $ $Revision: 5 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:b="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/brick.user.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        NXT Brick
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Brick
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Brick.
      </xsl:with-param>
      <xsl:with-param name="head">
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html" indent="yes"/>-->

  <xsl:template match="/b:NxtBrickState">
    <script language="javascript" type="text/javascript">
      <![CDATA[<!--
function findParentTag(element, tagName)
{
  if (element.tagName == tagName)
  {
    return element;
  }
  else
  {
    return findParentTag(element.parentNode, tagName);
  }
}

function expandoClick()
{
  var originRow = findParentTag(event.srcElement, "TR");
  var rootId = originRow.id;
  
  var icon = document.all(rootId + ".icon");
  var children = document.all(rootId + ".child");
  var display;
  
  if (icon.innerText == "t")
  {
    icon.innerText = "u";
    display = "block";
  }
  else
  {
    icon.innerText = "t";
    display = "none";
  }
  
  if (children.length == undefined)
  {
      children.style.display = display;
  }
  else
  {
    for (var i = 0; i < children.length; i++)
    {
      children[i].style.display = display;
    }
  }
}
      //-->]]>
    </script>
    <img src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtBrick.Image.png"
     align="right" alt="Buttons" width="32px" height="32px"/>

    <table width="90%">
      <xsl:apply-templates select="b:Runtime"/>
      <xsl:apply-templates select="b:Configuration"/>
      <xsl:apply-templates select="b:Runtime/b:Devices"/>
    </table>
  </xsl:template>

  <xsl:template match="b:Runtime">
    <tr>
      <th colspan="4">Configuration</th>
    </tr>
    <tr class="even">
      <th>Connected:</th>
      <td colspan="3">
        <xsl:value-of select="b:Connected"/>
      </td>
    </tr>
    <xsl:if test="b:Connected = 'true'">
      <tr class="odd">
        <th>Brick Name:</th>
        <td colspan="3">
          <xsl:value-of select="b:BrickName"/>
        </td>
      </tr>
      <tr class="even">
        <th>Firmware:</th>
        <td colspan="3">
          <xsl:value-of select="b:Firmware"/>
        </td>
      </tr>
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="b:Configuration">
    <tr class="odd">
      <th>Serial Port:</th>
      <td colspan="3">
        <xsl:value-of select="b:SerialPort"/>
      </td>
    </tr>
    <tr class="even">
      <th>Baud Rate:</th>
      <td colspan="3">
        <xsl:value-of select="b:BaudRate"/>
      </td>
    </tr>
    <tr class="odd">
      <th>Connection Type:</th>
      <td>
        <xsl:value-of select="b:ConnectionType"/>
      </td>
    </tr>
  </xsl:template>
  
  <xsl:template match="b:Runtime/b:Devices">
    <tr>
      <th colspan="4">Devices</th>
    </tr>
    <tr>
      <th>Port</th>
      <th>Type</th>
      <th>Model</th>
      <th>Service</th>
    </tr>
    
    <xsl:apply-templates select="b:Elem">
      <xsl:sort select="b:AttachRequest/b:Registration/b:Connection/b:Port"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="b:Elem">
    <xsl:variable name="class">
      <xsl:choose>
        <xsl:when test="position() mod 2 = 0">
          <xsl:text>even</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:text>odd</xsl:text>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="uri">
      <xsl:value-of select="substring-after(substring-after(b:AttachRequest/b:Registration/b:ServiceUri,'://'),'/')"/>
    </xsl:variable>
    
    <tr class="{$class}" style="cursor:hand;" id="expando{position()}" onclick="expandoClick()">
      <td>
        <span style="font-family:Marlett" id="expando{position()}.icon">t</span>
        <xsl:text> </xsl:text>
        <xsl:value-of select="b:AttachRequest/b:Registration/b:Connection/b:Port"/>
      </td>
      <td>
        <xsl:value-of select="b:AttachRequest/b:Registration/b:DeviceType"/>
      </td>
      <td>
        <xsl:value-of select="b:AttachRequest/b:Registration/b:DeviceModel"/>
      </td>
      <td>
        <a href="/{$uri}" >
          <xsl:call-template name="stripRightMost">
            <xsl:with-param name="input" select="$uri"/>
            <xsl:with-param name="sep" select="'/'"/>
          </xsl:call-template>
        </a>
      </td>
    </tr>
    <tr class="{$class}" style="display:None" id="expando{position()}.child">
      <td colspan="4">
        <a href="/{$uri}">
          <xsl:value-of select="$uri"/>
        </a>
      </td>
    </tr>
  </xsl:template>

  <xsl:template name="stripRightMost">
    <xsl:param name="input"/>
    <xsl:param name="sep"/>

    <xsl:value-of select="substring-before($input,$sep)"/>

    <xsl:variable name="remain" select="substring-after($input,$sep)"/>

    <xsl:if test="substring-before($remain,$sep) != ''">
      <xsl:value-of select="$sep"/>
      <xsl:call-template name="stripRightMost">
        <xsl:with-param name="input" select="$remain"/>
        <xsl:with-param name="sep" select="$sep"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>