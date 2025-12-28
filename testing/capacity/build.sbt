name := "naglfar-capacity-tests"
version := "1.0.0"
scalaVersion := "2.13.12"

enablePlugins(GatlingPlugin)

libraryDependencies += "io.gatling.highcharts" % "gatling-charts-highcharts" % "3.10.3" % "test"
libraryDependencies += "io.gatling" % "gatling-test-framework" % "3.10.3" % "test"
libraryDependencies += "org.yaml" % "snakeyaml" % "2.2"
libraryDependencies += "com.fasterxml.jackson.module" %% "jackson-module-scala" % "2.16.1"
libraryDependencies += "com.fasterxml.jackson.dataformat" % "jackson-dataformat-yaml" % "2.16.1"
