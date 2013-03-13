<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Accelerometer.user.xslt $ $Revision: 2 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:a="http://schemas.microsoft.com/robotics/2007/07/mindsensors/nxt/accelerometer.user.html"
                xmlns:c="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/common.user.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>MindSensors Accelerometer - </xsl:text>
        <xsl:value-of select="/a:AccelerometerState/a:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        MindSensors Accelerometer
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of a MindSensors Accelerometer.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/a:AccelerometerState/a:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/a:AccelerometerState/a:Tilt/c:TimeStamp"/>
          <xsl:text>";
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.Tilt.TimeStamp != timeStamp)
    {
      timeStamp = state.Tilt.TimeStamp;
      
      for (var key in state.Tilt)
      {
        var cell = document.all(key);
        if (cell != null)
        {
          cell.innerText = state.Tilt[key];
        }
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

          ]]>
        </script>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html"/>-->

  <xsl:template match="/a:AccelerometerState">
    <img src="/resources/User.MindSensors.Y2007.M07/Microsoft.Robotics.Services.Sample.MindSensors.Resources.MindSensorsAccelerationSensor.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th width="30%">Name:</th>
        <td width="70%">
          <xsl:value-of select="a:Name"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Connection:</th>
        <td>
          <xsl:choose>
            <xsl:when test="a:Connected = 'true'">
              Connected
            </xsl:when>
            <xsl:otherwise>
              Not connected
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> on </xsl:text>
          <xsl:value-of select="a:SensorPort"/>
        </td>
      </tr>
      <tr class="even">
        <th>X</th>
        <td id="X">
          <xsl:value-of select="a:Tilt/c:X"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Y</th>
        <td id="Y">
          <xsl:value-of select="a:Tilt/c:Y"/>
        </td>
      </tr>
      <tr class="even">
        <th>Z</th>
        <td id="Z">
          <xsl:value-of select="a:Tilt/c:Z"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Timestamp</th>
        <td id="TimeStamp">
          <xsl:value-of select="a:Tilt/c:TimeStamp"/>
        </td>
      </tr>
    </table>

  </xsl:template>
</xsl:stylesheet>