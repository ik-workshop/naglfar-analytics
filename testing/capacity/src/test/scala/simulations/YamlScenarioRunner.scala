package simulations

import io.gatling.core.Predef._
import io.gatling.http.Predef._
import scala.concurrent.duration._
import scala.jdk.CollectionConverters._
import java.io.{File, FileInputStream}
import org.yaml.snakeyaml.Yaml
import org.yaml.snakeyaml.constructor.Constructor
import simulations.models._

class YamlScenarioRunner extends Simulation {

  // Get scenario file from system property or default
  val scenarioFile = System.getProperty("scenario", "scenarios/browse-books.yaml")
  val scenarioPath = new File(scenarioFile)

  println(s"Loading scenario from: ${scenarioPath.getAbsolutePath}")

  // Load and parse YAML
  val yaml = new Yaml(new Constructor(classOf[ScenarioConfig]))
  val config = yaml.load(new FileInputStream(scenarioPath)).asInstanceOf[ScenarioConfig]

  println(s"Loaded scenario: ${config.name}")
  println(s"Description: ${config.description}")

  // Resolve base URL from environment or config
  val baseUrl = sys.env.getOrElse("BASE_URL", config.baseUrl)

  // HTTP protocol configuration
  val httpProtocol = http
    .baseUrl(baseUrl)
    .acceptHeader("application/json")
    .userAgentHeader("Gatling-Capacity-Test/1.0")

  // Build scenarios from YAML
  val gatlingScenarios = config.scenarios.asScala.map { userScenario =>
    val scenarioBuilder = scenario(userScenario.name)
      .exec(buildSteps(userScenario.steps.asScala.toSeq))

    scenarioBuilder
  }

  // Build injection profile from YAML
  val injectionSteps = config.injection.asScala.map { step =>
    step.`type` match {
      case "rampUsers" =>
        rampUsers(step.users).during(parseDuration(step.duration))
      case "constantUsersPerSec" =>
        constantUsersPerSec(step.rate).during(parseDuration(step.duration))
      case "atOnceUsers" =>
        atOnceUsers(step.users)
      case _ =>
        throw new IllegalArgumentException(s"Unknown injection type: ${step.`type`}")
    }
  }.toSeq

  // Set up scenarios with weights
  val weightedScenarios = gatlingScenarios.zip(config.scenarios.asScala).map { case (scenario, config) =>
    scenario.inject(injectionSteps: _*)
  }

  // Run simulation
  setUp(weightedScenarios: _*)
    .protocols(httpProtocol)
    .assertions(
      global.successfulRequests.percent.gte(config.thresholds.global.successRate),
      global.responseTime.max.lte(config.thresholds.global.maxResponseTime)
    )

  // Helper: Build scenario steps from YAML
  def buildSteps(steps: Seq[ScenarioStep]): ChainBuilder = {
    steps.foldLeft(exec(session => session)) { (chain, step) =>
      var stepChain = chain

      // Add pause if specified
      if (step.pause != null) {
        stepChain = stepChain.pause(parseDuration(step.pause))
      }

      // Add HTTP request if specified
      if (step.http != null) {
        val httpRequest = buildHttpRequest(step)

        // Apply condition if specified
        if (step.condition != null) {
          stepChain = stepChain.doIf(parseCondition(step.condition)) {
            exec(httpRequest)
          }
        } else {
          stepChain = stepChain.exec(httpRequest)
        }
      }

      stepChain
    }
  }

  // Helper: Build HTTP request from YAML
  def buildHttpRequest(step: ScenarioStep): ChainBuilder = {
    val req = step.http
    val name = if (step.name != null && step.name.nonEmpty) step.name else s"${req.method} ${req.path}"

    // Build base request
    var httpReq = req.method.toUpperCase match {
      case "GET" => http(name).get(interpolate(req.path))
      case "POST" => http(name).post(interpolate(req.path))
      case "PUT" => http(name).put(interpolate(req.path))
      case "DELETE" => http(name).delete(interpolate(req.path))
      case _ => throw new IllegalArgumentException(s"Unsupported HTTP method: ${req.method}")
    }

    // Add headers
    req.headers.asScala.foreach { case (key, value) =>
      httpReq = httpReq.header(key, interpolate(value))
    }

    // Add body if present
    if (req.body != null) {
      httpReq = httpReq.body(StringBody(interpolate(req.body)))
    }

    // Add checks
    req.checks.asScala.foreach { check =>
      if (check.status != null) {
        httpReq = httpReq.check(status.is(check.status))
      }
      if (check.jsonPath != null) {
        httpReq = httpReq.check(jsonPath(check.jsonPath).exists)
      }
    }

    // Save headers
    var chain = exec(httpReq)
    req.saveHeaders.asScala.foreach { save =>
      chain = chain.exec(session => {
        val headerValue = session("gatling.http.headers." + save.header).asOption[String]
        headerValue match {
          case Some(value) => session.set(save.name, value)
          case None => session
        }
      })
    }

    // Save JSON path values
    req.saveJsonPath.asScala.foreach { save =>
      chain = chain.exec(
        http(name).get(req.path)
          .check(jsonPath(save.path).saveAs(save.name))
      )
    }

    chain
  }

  // Helper: Parse duration string (e.g., "30s", "2m")
  def parseDuration(durationStr: String): FiniteDuration = {
    val pattern = """(\d+)([smh])""".r
    durationStr match {
      case pattern(amount, unit) =>
        val duration = amount.toInt
        unit match {
          case "s" => duration.seconds
          case "m" => duration.minutes
          case "h" => duration.hours
          case _ => throw new IllegalArgumentException(s"Unknown duration unit: $unit")
        }
      case _ => throw new IllegalArgumentException(s"Invalid duration format: $durationStr")
    }
  }

  // Helper: Parse condition expression
  def parseCondition(condition: String): Session => Boolean = {
    session => {
      // Simple null check: "varName != null"
      val notNullPattern = """(\w+)\s*!=\s*null""".r
      condition match {
        case notNullPattern(varName) =>
          session.contains(varName) && session(varName).asOption[String].isDefined
        case _ => true // Default to true for unsupported conditions
      }
    }
  }

  // Helper: Interpolate Gatling EL expressions
  def interpolate(str: String): String = {
    // Replace ${varName} with #{varName} for Gatling EL
    str.replaceAll("""\$\{(\w+)\}""", "#{$1}")
       .replaceAll("""\$\{random\((\d+),(\d+)\)\}""", "#{random($1,$2)}")
  }
}
