Feature: Notification Service
  As a Customer
  I want to receive an email notification when my ticket is issued
  So that I have a record of my purchase and my entry ticket

  Scenario: Successful ticket issued notification
    Given a ticket has been issued for an order
    And the ticket-issued event is published to Kafka
    When the Notification service consumes the ticket-issued event
    Then an email should be sent to the customer
    And the email should contain the ticket details and the PDF link
