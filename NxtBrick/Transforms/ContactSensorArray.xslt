<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: ContactSensorArray.xslt $ $Revision: 4 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:b="http://schemas.microsoft.com/robotics/2007/10/contactsensorarray.html">

  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        NXT Contact Sensor Array
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        LEGO NXT Contact Sensor Array (v2)
      </xsl:with-param>
      <xsl:with-param name="description">
        Configuration of an NXT Contact Sensor Array.
      </xsl:with-param>
      <xsl:with-param name="head">
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html" indent="yes"/>-->

  <xsl:template match="/b:NxtContactSensorArrayState">
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
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtContactSensorArray.Image.png"
     align="right" alt="Buttons" width="32px" height="32px"/>

    <table width="90%">
      <tr>
        <th colspan="5" align="center">Sensor Configuration</th>
      </tr>
      <tr>
        <th align="center">Required</th>
        <th align="center">Required</th>
        <th align="center">Optional</th>
        <th colspan="2" align="center">Success Range</th>
      </tr>
      <tr>
        <th align="center">Device Model:</th>
        <th align="center">Range Name:</th>
        <th align="center">Device Name:</th>
        <th align="center">Minimum:</th>
        <th align="center">Maximum:</th>
      </tr>
      <xsl:apply-templates select="b:SensorConfiguration/b:SensorConfiguration"/>
      <tr>
        <td> </td>
      </tr>
      <tr>
        <th colspan="5" align="center">Sensors Connected to the LEGO NXT Brick</th>
      </tr>
      <tr>
        <th align="center">Contact Sensor:</th>
        <th align="center">Connection:</th>
        <th align="center">Minimum:</th>
        <th align="center">Maximum:</th>
        <th align="center">Contact:</th>
      </tr>
      <xsl:apply-templates select="b:RuntimeConfiguration/b:Elem"/>
    </table>
  </xsl:template>

  <xsl:template match="b:RuntimeConfiguration/b:Elem">
    <tr>
      <td align="center">
        <xsl:value-of select="b:SensorRange/b:Model"/>.<xsl:value-of select="b:SensorRange/b:RangeName"/>
      </td>
      <td align="center">
        <xsl:if test="b:SensorRange/b:SensorName = ''">
          <xsl:value-of select="b:SensorRange/b:SensorPort"/>
        </xsl:if>
        <xsl:if test="b:SensorRange/b:SensorName != ''">
          <xsl:value-of select="b:SensorRange/b:SensorName"/> on <xsl:value-of select="b:SensorRange/b:SensorPort"/>
        </xsl:if>
      </td>
      <td align="center">
        <xsl:value-of select="b:PortConfiguration/b:SuccessRangeMin"/>
      </td>
      <td align="center">
        <xsl:value-of select="b:PortConfiguration/b:SuccessRangeMax"/>
      </td>
      <td align="center">
        <xsl:value-of select="b:PortConfiguration/b:Contact"/>
      </td>
    </tr>
  </xsl:template>

  <xsl:template match="b:SensorConfiguration">
    <tr>
      <td align="center">
        <xsl:value-of select="b:DeviceModel"/>
      </td>
      <td align="center">
        <xsl:value-of select="b:RangeName"/>
      </td>
      <td align="center">
        <xsl:if test="b:DeviceName = ''">(any)</xsl:if>
        <xsl:if test="b:DeviceName != ''">
          <xsl:value-of select="b:DeviceName"/>
        </xsl:if>
      </td>
      <td align="center">
        <xsl:value-of select="b:SuccessRangeMin"/>
      </td>
      <td align="center">
        <xsl:value-of select="b:SuccessRangeMax"/>
      </td>
    </tr>
  </xsl:template>

</xsl:stylesheet>