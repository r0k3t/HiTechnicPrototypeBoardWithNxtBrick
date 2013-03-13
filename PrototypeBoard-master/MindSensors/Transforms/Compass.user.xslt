<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Compass.user.xslt $ $Revision: 4 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:c="http://schemas.microsoft.com/robotics/2007/08/mindsensors/nxt/compasssensor.user.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>MindSensors Compass - </xsl:text>
        <xsl:value-of select="/c:CompassSensorState/c:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        MindSensors Compass
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of a MindSensors Compass.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/c:CompassSensorState/c:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/c:CompassSensorState/c:Heading/c:TimeStamp"/>
          <xsl:text>";
var heading = </xsl:text>
          <xsl:value-of select="/c:CompassSensorState/c:Heading/c:Degrees"/>
          <xsl:text>;
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.Heading.TimeStamp != timeStamp)
    {
      timeStamp = state.Heading.TimeStamp;
      
      for (var key in state.Heading)
      {
        var cell = document.all(key);
        if (cell != null)
        {
          cell.innerText = state.Heading[key];
        }
      }
      
      if (heading != parseFloat(state.Heading.Degrees))
      {
        heading = parseFloat(state.Heading.Degrees);
        setNewTarget(heading);
      }
    }
    readState(pollingFrequency);
}

function startPolling()
{
  setStateCompletion("onState");
  readState(pollingFrequency);
}

setTimeout("startPolling()", 1000);

var current = 180;
var target = 0;
var turning = false;

function setNewTarget(angle)
{
    target = clip(parseFloat(angle));
    
    if (!turning)
    {
        turning = true;
        setTimeout("setRotation()", 25);
    }
}

function clip(value)
{
    while (value > 180)
    {
        value -= 360;
    }
    
    while (value < -180)
    {
        value += 360;
    }
    
    return value;
}

function setRotation()
{
    var delta = clip(target - current);
    
    if (delta > 20)
    {
        delta = 20;
    }
    else if (delta < -20)
    {
        delta = -20;
    }
    else if (delta == 0)
    {
        turning = false;
        return;
    }
    
    current = clip(current + delta);

    var angle = current;    
    
    var compass = document.all("compass");
    var filter = compass.filters[0];
    
    // note: use -ve angle, because compasses are clockwise
    var radians = -angle * Math.PI / 180;
    var cosTheta = Math.cos(radians);
    var sinTheta = Math.sin(radians);
    
    filter.M11 = cosTheta;
    filter.M12 = -sinTheta;
    filter.M21 = sinTheta;
    filter.M22 = cosTheta;
    
    //debugger
    
    filter.Dx = 64 * (1 - cosTheta + sinTheta);
    filter.Dy = 64 * (1 - cosTheta - sinTheta);
    
    setTimeout("setRotation()", 25);
}    

          ]]>
        </script>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html"/>-->

  <xsl:template match="/c:CompassSensorState">
    <img src="/resources/User.MindSensors.Y2007.M07/Microsoft.Robotics.Services.Sample.MindSensors.Resources.MindSensorsCompassSensor.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th width="30%">Name:</th>
        <td width="70%">
          <xsl:value-of select="c:Name"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Connection:</th>
        <td>
          <xsl:choose>
            <xsl:when test="c:Connected = 'true'">
              Connected
            </xsl:when>
            <xsl:otherwise>
              Not connected
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> on </xsl:text>
          <xsl:value-of select="c:SensorPort"/>
        </td>
      </tr>
      <tr class="even">
        <th>Heading</th>
        <td id="Degrees">
          <xsl:value-of select="c:Heading/c:Degrees"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Timestamp</th>
        <td id="TimeStamp">
          <xsl:value-of select="c:Heading/c:TimeStamp"/>
        </td>
      </tr>
      <tr>
        <td colspan="2">
          <div style="width:128px;height:128px;background:white;">
            <div id="compass" style="width:128px;height:128px;filter:progid:DXImageTransform.Microsoft.Matrix(FilterType='nearest neighbor');">
              <img src="/resources/User.MindSensors.Y2007.M07/Microsoft.Robotics.Services.Sample.MindSensors.Transforms.Compass.png"
                   width="128px" height="128px"
                   onload="setNewTarget({c:Heading/c:Degrees});"/>
            </div>
          </div>
        </td>
      </tr>
    </table>
    <br/>
    There exists a deviation between Magnetic North and Geographic North which varies by region and changes over time.<br/>
    For more information, please refer to the following resources:<br/>
    <br/>
    National Geomagnetism Program - <a href="http://geomag.usgs.gov/faqs.php#four">What is declination?</a><br/>
    National Geophysical Data Center - <a href="http://www.ngdc.noaa.gov/seg/geomag/jsp/struts/calcDeclination">Estimated Value of Magnetic Declination</a><br/>
    <br/>
  </xsl:template>
</xsl:stylesheet>