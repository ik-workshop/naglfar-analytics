FROM sbtscala/scala-sbt:graalvm-community-21.0.2_1.11.7_3.3.7

WORKDIR /capacity

# Copy SBT build files
COPY build.sbt .
COPY project/build.properties project/
COPY project/plugins.sbt project/

# Download dependencies
RUN sbt update

# Copy source code and scenarios
COPY src/ src/
COPY scenarios/ scenarios/

# Compile the project
RUN sbt test:compile

# Create results directory
RUN mkdir -p results

# Default: run browse books scenario
# Override with: docker run ... -Dscenario=scenarios/full-user-flow.yaml
ENTRYPOINT ["sbt"]
CMD ["gatling:test", "-Dscenario=scenarios/browse-books.yaml"]
