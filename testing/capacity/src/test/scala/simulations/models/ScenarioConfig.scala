package simulations.models

import scala.beans.BeanProperty

// Root configuration
case class ScenarioConfig(
  @BeanProperty var name: String = "",
  @BeanProperty var description: String = "",
  @BeanProperty var baseUrl: String = "http://localhost",
  @BeanProperty var injection: java.util.List[InjectionStep] = new java.util.ArrayList[InjectionStep](),
  @BeanProperty var thresholds: Thresholds = Thresholds(),
  @BeanProperty var scenarios: java.util.List[UserScenario] = new java.util.ArrayList[UserScenario]()
)

// Injection step configuration
case class InjectionStep(
  @BeanProperty var `type`: String = "",
  @BeanProperty var users: Int = 0,
  @BeanProperty var duration: String = "",
  @BeanProperty var rate: Int = 0
)

// Threshold configuration
case class Thresholds(
  @BeanProperty var global: GlobalThreshold = GlobalThreshold(),
  @BeanProperty var requests: java.util.Map[String, RequestThreshold] = new java.util.HashMap[String, RequestThreshold]()
)

case class GlobalThreshold(
  @BeanProperty var successRate: Double = 95.0,
  @BeanProperty var maxResponseTime: Int = 2000
)

case class RequestThreshold(
  @BeanProperty var p95: Int = 1000,
  @BeanProperty var p99: Int = 2000,
  @BeanProperty var successRate: Double = 95.0
)

// User scenario
case class UserScenario(
  @BeanProperty var name: String = "",
  @BeanProperty var weight: Int = 100,
  @BeanProperty var steps: java.util.List[ScenarioStep] = new java.util.ArrayList[ScenarioStep]()
)

// Scenario step
case class ScenarioStep(
  @BeanProperty var name: String = "",
  @BeanProperty var condition: String = null,
  @BeanProperty var pause: String = null,
  @BeanProperty var http: HttpRequest = null
)

// HTTP request configuration
case class HttpRequest(
  @BeanProperty var method: String = "GET",
  @BeanProperty var path: String = "",
  @BeanProperty var headers: java.util.Map[String, String] = new java.util.HashMap[String, String](),
  @BeanProperty var body: String = null,
  @BeanProperty var checks: java.util.List[Check] = new java.util.ArrayList[Check](),
  @BeanProperty var saveHeaders: java.util.List[SaveHeader] = new java.util.ArrayList[SaveHeader](),
  @BeanProperty var saveJsonPath: java.util.List[SaveJsonPath] = new java.util.ArrayList[SaveJsonPath]()
)

// Check configuration
case class Check(
  @BeanProperty var status: Integer = null,
  @BeanProperty var jsonPath: String = null,
  @BeanProperty var responseTime: ResponseTimeCheck = null
)

case class ResponseTimeCheck(
  @BeanProperty var p95: Int = 0,
  @BeanProperty var p99: Int = 0
)

// Save configurations
case class SaveHeader(
  @BeanProperty var name: String = "",
  @BeanProperty var header: String = ""
)

case class SaveJsonPath(
  @BeanProperty var name: String = "",
  @BeanProperty var path: String = ""
)
