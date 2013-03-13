<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Motor.xslt $ $Revision: 4 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:m="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/motor.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>NXT Motor - </xsl:text>
        <xsl:value-of select="/m:MotorState/m:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Motor
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Motor.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/m:MotorState/m:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/m:MotorState/m:CurrentEncoderTimeStamp"/>
          <xsl:text>";
var angle = </xsl:text>
          <xsl:value-of select="/m:MotorState/m:CurrentEncoderDegrees"/>
          <xsl:text>;
var rpm = 0;
var spinning = false;
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.CurrentEncoderTimeStamp != timeStamp)
    {
      timeStamp = state.CurrentEncoderTimeStamp;
      
      for (var key in state)
      {
        var cell = document.all(key);
        if (cell != null)
        {
          cell.innerText = state[key];
        }
      }
      
      if (angle != parseInt(state.CurrentEncoderDegrees))
      {
        angle = parseInt(state.CurrentEncoderDegrees);
        setWheelRotation(angle);
      }
      rpm = state.CurrentMotorRpm;
      if (Math.abs(rpm) < 10)
      {
        spinning = false;
      }
      else if (!spinning)
      {
        spinning = true;
        setTimeout("spinTheWheel()", 25);
      }
    }
    readState(pollingFrequency);
}

function spinTheWheel()
{
  if (!spinning)
  {
    return;
  }
  
  angle = angle + rpm * 6 * 25 / 1000;
  setWheelRotation(angle);
  
  setTimeout("spinTheWheel()", 25);
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

  <xsl:template match="/m:MotorState">
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtMotor.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th width="30%">Name:</th>
        <td width="70%">
          <xsl:value-of select="m:Name"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Connection:</th>
        <td>
          <xsl:choose>
            <xsl:when test="m:Connected = 'true'">
              Connected
            </xsl:when>
            <xsl:otherwise>
              Not connected
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> on </xsl:text>
          <xsl:value-of select="m:MotorPort"/>
        </td>
      </tr>
      <tr class="even">
        <th>Polarity:</th>
        <td>
          <xsl:choose>
            <xsl:when test="m:ReversePolarity = 'true'">
              Reversed
            </xsl:when>
            <xsl:otherwise>
              Normal
            </xsl:otherwise>
          </xsl:choose>
        </td>
      </tr>
      <tr class="odd">
        <th>Current RPM</th>
        <td id="CurrentMotorRpm">
          <xsl:value-of select="m:CurrentMotorRpm"/>
        </td>
      </tr>
      <tr class="even">
        <th>Target Power</th>
        <td id="TargetPower">
          <xsl:value-of select="m:TargetPower"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Current Power</th>
        <td id="CurrentPower">
          <xsl:value-of select="m:CurrentPower"/>
        </td>
      </tr>
      <tr class="even">
        <th>Target Encoder Degrees</th>
        <td id="TargetEncoderDegrees">
          <xsl:value-of select="m:TargetEncoderDegrees"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Current Encoder Degrees</th>
        <td id="CurrentEncoderDegrees">
          <xsl:value-of select="m:CurrentEncoderDegrees"/>
        </td>
      </tr>
      <tr class="even">
        <th>Resetable Encoder Degrees</th>
        <td id="ResetableEncoderDegrees">
          <xsl:value-of select="m:ResetableEncoderDegrees"/>
        </td>
      </tr>
      <tr class="odd">
        <th>
          Target Stop State
        </th>
        <td id="TargetStopState">
          <xsl:value-of select="m:TargetStopState"/>
        </td>
      </tr>
      <tr class="even">
        <th>Timestamp</th>
        <td id="CurrentEncoderTimeStamp">
          <xsl:value-of select="m:CurrentEncoderTimeStamp"/>
        </td>
      </tr>
      <tr>
        <td colspan="2">
          <script language="javascript" type="text/javascript">
            <![CDATA[<!--
            
function setWheelRotation(degrees)
{
  var radians = degrees * Math.PI / 180;
  var cosTheta = Math.cos(radians);
  var sinTheta = Math.sin(radians);
  
  var wheel = document.all("wheel");
  var filter = wheel.filters.item(0);
  
  filter.M11 = cosTheta;
  filter.M12 = -sinTheta;
  filter.M21 = sinTheta;
  filter.M22 = cosTheta;

  filter.Dx = 64 * (1 -  cosTheta + sinTheta);
  filter.Dy = 64 * (1 -  cosTheta - sinTheta);
}
            //-->]]>
          </script>
          <div style="width:128px;height:128px;background:white;">
            <div id="wheel" style="width:128px;height:128px;filter:progid:DXImageTransform.Microsoft.Matrix(FilterType='nearest neighbor');">
              <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Wheel.png"
                   width="128px" height="128px"
                   onload="setWheelRotation({m:CurrentEncoderDegrees});"/>
            </div>
          </div>
        </td>
      </tr>
    </table>

  </xsl:template>
</xsl:stylesheet>