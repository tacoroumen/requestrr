/*!

=========================================================
* Argon Dashboard React - v1.0.0
=========================================================

* Product Page: https://www.creative-tim.com/product/argon-dashboard-react
* Copyright 2019 Creative Tim (https://www.creative-tim.com)
* Licensed under MIT (https://github.com/creativetimofficial/argon-dashboard-react/blob/master/LICENSE.md)

* Coded by Creative Tim

=========================================================

* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

*/
import {useEffect, useState} from "react";
import {Oval} from 'react-loader-spinner'
import {useDispatch} from 'react-redux';
import {Alert} from "reactstrap";
import {testSettings, testMatrixSettings} from "../store/actions/ChatClientsActions"
import {getSettings} from "../store/actions/ChatClientsActions"
import {save} from "../store/actions/ChatClientsActions"
import MultiDropdown from "../components/Inputs/MultiDropdown"
import Dropdown from "../components/Inputs/Dropdown"

// reactstrap components
import {
  Button,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Input,
  Container,
  Row,
  Col
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";


function ChatClients(props) {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [isCopyingLink, setIsCopyingLink] = useState(false);
  const [isTestingSettings, setIsTestingSettings] = useState(false);
  const [testSettingsRequested, setTestSettingsRequested] = useState(false);
  const [testSettingsSuccess, setTestSettingsSuccess] = useState(false);
  const [testSettingsError, setTestSettingsError] = useState("");
  const [monitoredChannels, setMonitoredChannels] = useState([]);
  const [statusMessage, setStatusMessage] = useState("");
  const [enableRequestsThroughDirectMessages, setEnableRequestsThroughDirectMessages] = useState(true);
  const [chatClient, setChatClient] = useState("");
  const [chatClientChanged, setChatClientChanged] = useState(false);
  const [chatClientInvalid, setChatClientInvalid] = useState(false);
  const enabledClients = (chatClient || "").split(",").map(x => x.trim()).filter(x => x);
  const discordEnabled = enabledClients.includes("Discord");
  const matrixEnabled = enabledClients.includes("Matrix");
  const setClients = (discord, matrix) => {
    setChatClient([discord ? "Discord" : null, matrix ? "Matrix" : null].filter(x => x).join(","));
    setChatClientChanged(true);
  };
  const [clientId, setClientId] = useState("");
  const [clientIdChanged, setClientIdChanged] = useState(false);
  const [clientIdInvalid, setClientIdInvalid] = useState(false);
  const [botToken, setBotToken] = useState("");
  const [botTokenChanged, setBotTokenChanged] = useState(false);
  const [botTokenInvalid, setBotTokenInvalid] = useState(false);
  const [tvShowRoles, setTvShowRoles] = useState([]);
  const [movieRoles, setMovieRoles] = useState([]);
  const [musicRoles, setMusicRoles] = useState([]);

  const [requestApprovalEnabled, setRequestApprovalEnabled] = useState(false);
  const [requestApprovalAdminRoleIds, setRequestApprovalAdminRoleIds] = useState([]);
  const [requestApprovalAdminChannelIds, setRequestApprovalAdminChannelIds] = useState([]);
  const [requestApprovalAdminChannelAllRequests, setRequestApprovalAdminChannelAllRequests] = useState(false);
  const [requestApprovalEmojiApprove, setRequestApprovalEmojiApprove] = useState("✅");
  const [requestApprovalEmojiDeny, setRequestApprovalEmojiDeny] = useState("❌");

  const [automaticallyNotifyRequesters, setAutomaticallyNotifyRequesters] = useState(true);
  const [notificationMode, setNotificationMode] = useState("PrivateMessages");
  const [notificationChannels, setNotificationChannels] = useState([]);
  const [automaticallyPurgeCommandMessages, setAutomaticallyPurgeCommandMessages] = useState(true);
  const [matrixHomeserverUrl, setMatrixHomeserverUrl] = useState("");
  const [matrixAccessToken, setMatrixAccessToken] = useState("");
  const [matrixCommandPrefix, setMatrixCommandPrefix] = useState("!");
  const [matrixMonitoredRooms, setMatrixMonitoredRooms] = useState([]);
  const [matrixNotificationMode, setMatrixNotificationMode] = useState("PrivateMessages");
  const [matrixNotificationRooms, setMatrixNotificationRooms] = useState([]);
  const [matrixAutomaticallyNotifyRequesters, setMatrixAutomaticallyNotifyRequesters] = useState(true);
  const [matrixApprovalRooms, setMatrixApprovalRooms] = useState([]);
  const [matrixApprovalPowerLevel, setMatrixApprovalPowerLevel] = useState("50");
  const [matrixApprovalRoomsAllRequests, setMatrixApprovalRoomsAllRequests] = useState(false);
  const [matrixMovieRequestLevels, setMatrixMovieRequestLevels] = useState([]);
  const [matrixTvShowRequestLevels, setMatrixTvShowRequestLevels] = useState([]);
  const [matrixMusicRequestLevels, setMatrixMusicRequestLevels] = useState([]);
  const [matrixHomeserverUrlInvalid, setMatrixHomeserverUrlInvalid] = useState(false);
  const [isTestingMatrix, setIsTestingMatrix] = useState(false);
  const [matrixTestRequested, setMatrixTestRequested] = useState(false);
  const [matrixTestSuccess, setMatrixTestSuccess] = useState(false);
  const [matrixTestError, setMatrixTestError] = useState("");
  const [matrixAccessTokenInvalid, setMatrixAccessTokenInvalid] = useState(false);
  const [language, setLanguage] = useState("english");
  const [availableLanguages, setAvailableLanguages] = useState([]);

  const dispatch = useDispatch();


  useEffect(() => {
    dispatch(getSettings(props.token))
      .then(data => {
        setIsLoading(false);
        setChatClient(data.payload.client);
        setClientId(data.payload.clientId);
        setBotToken(data.payload.botToken);
        setStatusMessage(data.payload.statusMessage);
        setMonitoredChannels(data.payload.monitoredChannels);
        setEnableRequestsThroughDirectMessages(data.payload.enableRequestsThroughDirectMessages);
        setTvShowRoles(data.payload.tvShowRoles);
        setMovieRoles(data.payload.movieRoles);
        setMusicRoles(data.payload.musicRoles);
        setRequestApprovalEnabled(data.payload.requestApprovalEnabled)
        setRequestApprovalAdminRoleIds(data.payload.requestApprovalAdminRoleIds || []);
        setRequestApprovalAdminChannelIds(data.payload.requestApprovalAdminChannelIds || []);
        setRequestApprovalAdminChannelAllRequests(data.payload.requestApprovalAdminChannelAllRequests || false);
        setRequestApprovalEmojiApprove(data.payload.requestApprovalEmojiApprove || "✅");
        setRequestApprovalEmojiDeny(data.payload.requestApprovalEmojiDeny || "❌");
        setAutomaticallyNotifyRequesters(data.payload.automaticallyNotifyRequesters);
        setNotificationMode(data.payload.notificationMode);
        setNotificationChannels(data.payload.notificationChannels);
        setAutomaticallyPurgeCommandMessages(data.payload.automaticallyPurgeCommandMessages);
        const matrix = data.payload.matrix || {};
        setMatrixHomeserverUrl(matrix.homeserverUrl || "");
        setMatrixAccessToken(matrix.accessToken || "");
        setMatrixCommandPrefix(matrix.commandPrefix || "!");
        setMatrixMonitoredRooms(matrix.monitoredRooms || []);
        setMatrixNotificationMode(matrix.notificationMode || "PrivateMessages");
        setMatrixNotificationRooms(matrix.notificationRooms || []);
        setMatrixAutomaticallyNotifyRequesters(matrix.automaticallyNotifyRequesters !== false);
        setMatrixApprovalRooms(matrix.approvalRooms || []);
        setMatrixApprovalPowerLevel(String(matrix.approvalPowerLevel ?? 50));
        setMatrixApprovalRoomsAllRequests(matrix.approvalRoomsAllRequests || false);
        setMatrixMovieRequestLevels(matrix.movieRequestLevels || []);
        setMatrixTvShowRequestLevels(matrix.tvShowRequestLevels || []);
        setMatrixMusicRequestLevels(matrix.musicRequestLevels || []);
        setLanguage(data.payload.language);
        setAvailableLanguages(data.payload.availableLanguages);
      });
  }, []);


  useEffect(() => {
    if (chatClientChanged)
      triggerChatClientValidation();
  }, [chatClientChanged, chatClient]);


  useEffect(() => {
    if (clientIdChanged)
      triggerClientIdValidation()
  }, [clientIdChanged]);


  useEffect(() => {
    if (botTokenChanged)
      triggerBotTokenValidation()
  }, [botTokenChanged]);


  const validateChatClient = () => {
    return /\S/.test(chatClient);
  };

  const validateClientId = () => {
    return /\S/.test(clientId);
  };

  const validateBotToken = () => {
    return /\S/.test(botToken);
  };


  // const onChatClientChange = (event) => {
  //   setChatClient(event.target.value);
  //   setChatClientChanged(true);
  // };

  const triggerChatClientValidation = () => {
    setChatClientInvalid(!validateChatClient());
  };

  const onStatusMessageChange = (event) => {
    setStatusMessage(event.target.value);
  };

  const onClientIdChange = (event) => {
    setClientId(event.target.value);
    setClientIdChanged(true);
  };


  const triggerClientIdValidation = () => {
    setClientIdInvalid(!validateClientId());
  };


  const onBotTokenChange = (event) => {
    setBotToken(event.target.value);
    setBotTokenChanged(true);
  };

  const triggerBotTokenValidation = () => {
    setBotTokenInvalid(!validateBotToken());
  };


  const validateMatrixHomeserverUrl = () => {
    return /^https?:\/\/\S+$/i.test(matrixHomeserverUrl.trim());
  };

  const validateMatrixAccessToken = () => {
    return /\S/.test(matrixAccessToken);
  };

  const validateMatrixFields = () => {
    setMatrixHomeserverUrlInvalid(!validateMatrixHomeserverUrl());
    setMatrixAccessTokenInvalid(!validateMatrixAccessToken());
    return validateMatrixHomeserverUrl() && validateMatrixAccessToken();
  };

  const validateSelectedClient = () => {
    let valid = true;

    if (matrixEnabled) {
      valid = validateMatrixFields() && valid;
    }

    if (discordEnabled) {
      triggerClientIdValidation();
      triggerBotTokenValidation();
      valid = validateBotToken() && validateClientId() && valid;
    }

    return valid;
  };

  const onTestMatrixSettings = (e) => {
    e.preventDefault();

    if (!isTestingMatrix && validateMatrixFields()) {
      setIsTestingMatrix(true);

      dispatch(testMatrixSettings({
        homeserverUrl: matrixHomeserverUrl.trim(),
        accessToken: matrixAccessToken.trim(),
      }))
        .then(data => {
          setIsTestingMatrix(false);
          setMatrixTestRequested(true);

          if (data.ok) {
            setMatrixTestError("");
            setMatrixTestSuccess(true);
          } else {
            setMatrixTestError(typeof (data.error) === "string" ? data.error : "An unknown error occurred while testing the settings");
            setMatrixTestSuccess(false);
          }
        });
    }
  };

  const onSaving = (e) => {
    e.preventDefault();

    triggerChatClientValidation();

    if (!isSaving) {
      if (validateChatClient()
        && validateSelectedClient()) {
        setIsSaving(true);

        dispatch(save({
          client: chatClient,
          clientId: clientId,
          botToken: botToken,
          statusMessage: statusMessage,
          monitoredChannels: monitoredChannels,
          tvShowRoles: tvShowRoles,
          movieRoles: movieRoles,
          musicRoles: musicRoles,
          requestApprovalEnabled: requestApprovalEnabled,
          requestApprovalAdminRoleIds: requestApprovalAdminRoleIds,
          requestApprovalAdminChannelIds: requestApprovalAdminChannelIds,
          requestApprovalAdminChannelAllRequests: requestApprovalAdminChannelAllRequests,
          requestApprovalEmojiApprove: requestApprovalEmojiApprove,
          requestApprovalEmojiDeny: requestApprovalEmojiDeny,
          enableRequestsThroughDirectMessages: enableRequestsThroughDirectMessages,
          automaticallyNotifyRequesters: automaticallyNotifyRequesters,
          notificationMode: notificationMode,
          notificationChannels: notificationChannels,
          automaticallyPurgeCommandMessages: automaticallyPurgeCommandMessages,
          matrix: {
            homeserverUrl: matrixHomeserverUrl.trim(),
            accessToken: matrixAccessToken.trim(),
            commandPrefix: matrixCommandPrefix.trim() || "!",
            monitoredRooms: matrixMonitoredRooms,
            notificationMode: matrixNotificationMode,
            notificationRooms: matrixNotificationRooms,
            automaticallyNotifyRequesters: matrixAutomaticallyNotifyRequesters,
            approvalRooms: matrixApprovalRooms,
            movieRequestLevels: matrixMovieRequestLevels,
            tvShowRequestLevels: matrixTvShowRequestLevels,
            musicRequestLevels: matrixMusicRequestLevels,
            approvalPowerLevel: /^\d+$/.test(matrixApprovalPowerLevel.trim()) ? Number(matrixApprovalPowerLevel.trim()) : 50,
            approvalRoomsAllRequests: matrixApprovalRoomsAllRequests,
          },
          language: language,
        }))
          .then(data => {
            setIsSaving(false);

            if (data.ok) {
              setSaveAttempted(true);
              setSaveError("");
              setSaveSuccess(true);
            } else {
              let error = "An unknown error occurred while saving.";

              if (typeof (data.error) === "string")
                error = data.error;

              setSaveAttempted(true);
              setSaveError(error);
              setSaveSuccess(false);
            }
          });
      } else {
        setSaveAttempted(true);
        setSaveError("Some fields are invalid, please fix them before saving.");
        setSaveSuccess(false);
      }
    }
  }

  const onTestSettings = (e) => {
    e.preventDefault();

    triggerChatClientValidation();
    triggerClientIdValidation();
    triggerBotTokenValidation();

    if (!isTestingSettings
      && validateChatClient()
      && validateBotToken()
      && validateClientId()) {
      setIsTestingSettings(true);

      dispatch(testSettings({
        chatClient: chatClient,
        clientId: clientId,
        botToken: botToken,
      }))
        .then(data => {
          setIsTestingSettings(false);

          if (data.ok) {
            setTestSettingsRequested(true);
            setTestSettingsError("");
            setTestSettingsSuccess(true);
          } else {
            let error = "An unknown error occurred while testing the settings";

            if (typeof (data.error) === "string")
              error = data.error;

            setTestSettingsRequested(true);
            setTestSettingsError(error);
            setTestSettingsSuccess(false);
          }
        });
    }
  };

  const onGenerateInviteLink = (e) => {
    e.preventDefault();

    triggerChatClientValidation();
    triggerClientIdValidation();
    triggerBotTokenValidation();

    if (!isCopyingLink
      && validateChatClient()
      && validateBotToken()
      && validateClientId()) {
      setIsCopyingLink(true);

      let linkElement = document.getElementById("discordlink");
      linkElement.classList.remove("d-none");
      linkElement.focus();
      linkElement.select();
      let text = linkElement.value;
      navigator.clipboard.writeText(text);
      linkElement.classList.add("d-none");

      // let thisRef = this;
      setTimeout(() => {
        setIsCopyingLink(false)
      }, 3000);
    }
  };


  return (
    <>
      <UserHeader title="Chat Client"
                  description="This page is for configuring your bot to your favorite chatting application"/>
      <Container className="mt--7" fluid>
        <Row>
          <Col className="order-xl-1" xl="12">
            <Card className="bg-secondary shadow">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Configuration</h3>
                  </Col>
                  <Col className="text-right" xs="4">
                    <Button
                      color="primary"
                      href="#pablo"
                      onClick={e => e.preventDefault()}
                      size="sm"
                    >
                      Settings
                    </Button>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody className={isLoading ? "fade" : "fade show"}>
                <Form className="complex">
                  <h6 className="heading-small text-muted mb-4">
                    General Settings
                  </h6>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <label className="form-control-label">
                            Chat Clients
                          </label>
                          <div className="custom-control custom-control-alternative custom-checkbox mb-2">
                            <Input
                              className="custom-control-input"
                              id="enableDiscord"
                              type="checkbox"
                              onChange={() => setClients(!discordEnabled, matrixEnabled)}
                              checked={discordEnabled}
                            />
                            <label className="custom-control-label" htmlFor="enableDiscord">
                              <span className="text-muted">Discord</span>
                            </label>
                          </div>
                          <div className="custom-control custom-control-alternative custom-checkbox mb-2">
                            <Input
                              className="custom-control-input"
                              id="enableMatrix"
                              type="checkbox"
                              onChange={() => setClients(discordEnabled, !matrixEnabled)}
                              checked={matrixEnabled}
                            />
                            <label className="custom-control-label" htmlFor="enableMatrix">
                              <span className="text-muted">Matrix</span>
                            </label>
                          </div>
                          {
                            chatClientInvalid ? (
                                <Alert className="mt-3" color="warning">
                                  <strong>Turn on at least one chat client.</strong>
                                </Alert>)
                              : null
                          }
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                  {matrixEnabled ? (
                    <>
                  <hr className="my-4"/>
                  <div>
                    <h6 className="heading-small text-muted mb-4">
                      Matrix Bot Settings
                    </h6>
                  </div>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <FormGroup className={matrixHomeserverUrlInvalid ? "has-danger" : ""}>
                          <label className="form-control-label" htmlFor="input-matrix-homeserver">
                            Homeserver URL
                          </label>
                          <Input
                            value={matrixHomeserverUrl} onChange={e => setMatrixHomeserverUrl(e.target.value)}
                            className="form-control-alternative"
                            id="input-matrix-homeserver"
                            placeholder="https://matrix.example.org"
                            type="text"
                          />
                          {
                            matrixHomeserverUrlInvalid ? (
                                <Alert className="mt-3" color="warning">
                                  <strong>Enter the homeserver URL, starting with https:// or http://.</strong>
                                </Alert>)
                              : null
                          }
                        </FormGroup>
                      </Col>
                      <Col lg="6">
                        <FormGroup className={matrixAccessTokenInvalid ? "has-danger" : ""}>
                          <label className="form-control-label" htmlFor="input-matrix-token">
                            Bot Access Token
                          </label>
                          <Input
                            value={matrixAccessToken} onChange={e => setMatrixAccessToken(e.target.value)}
                            className="form-control-alternative"
                            id="input-matrix-token"
                            placeholder="Access token of the bot account"
                            type="password"
                          />
                          {
                            matrixAccessTokenInvalid ? (
                                <Alert className="mt-3" color="warning">
                                  <strong>Access token is required.</strong>
                                </Alert>)
                              : null
                          }
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <label className="form-control-label" htmlFor="input-matrix-prefix">
                            Command Prefix
                          </label>
                          <Input
                            value={matrixCommandPrefix} onChange={e => setMatrixCommandPrefix(e.target.value)}
                            className="form-control-alternative"
                            id="input-matrix-prefix"
                            placeholder="!"
                            type="text"
                          />
                        </FormGroup>
                      </Col>
                      <Col lg="6">
                        <FormGroup>
                          <MultiDropdown
                            name="Rooms to monitor (empty: any room the bot is in)"
                            create={true}
                            searchable={true}
                            placeholder="Enter room ids (!abc:server) or aliases (#room:server)"
                            labelField="name"
                            valueField="id"
                            dropdownHandle={false}
                            selectedItems={matrixMonitoredRooms.map(x => {
                              return {name: x, id: x}
                            })}
                            items={matrixMonitoredRooms.map(x => {
                              return {name: x, id: x}
                            })}
                            onChange={newRooms => setMatrixMonitoredRooms(newRooms.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                          <small className="form-text text-muted">
                            In Element, open Room settings, Advanced, and copy the Internal room ID (looks like !abc123:your-domain). An alias such as #requests:your-domain also works.
                            Then invite the bot to the room; it joins on its own.
                          </small>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col md="12">
                        <p className="text-muted text-sm">
                          Create a separate Matrix account for the bot and paste its access token here. The bot works in unencrypted rooms only.
                          Without monitored rooms, the bot joins rooms when a user on its own homeserver invites it.
                        </p>
                      </Col>
                    </Row>
                    <Row>
                      <Col>
                        <FormGroup className="mt-2">
                          {
                            matrixTestRequested && !isTestingMatrix ?
                              !matrixTestSuccess ? (
                                  <Alert className="text-center" color="danger">
                                    <strong>{matrixTestError}</strong>
                                  </Alert>)
                                : <Alert className="text-center" color="success">
                                  <strong>Signed in to Matrix successfully.</strong>
                                </Alert>
                              : null
                          }
                        </FormGroup>
                        <FormGroup className="text-right">
                          <button onClick={onTestMatrixSettings} disabled={isTestingMatrix}
                                  className="btn btn-icon btn-3 btn-default" type="button">
                            <span className="btn-inner--icon">
                              {
                                isTestingMatrix ? (
                                  <Oval
                                    wrapperClass="loader"
                                    type="Oval"
                                    color="#11cdef"
                                    height={19}
                                    width={19}
                                  />)
                                  : (<i className="fas fa-cogs"></i>)
                              }</span>
                            <span className="btn-inner--text">Test Settings</span>
                          </button>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                  <hr className="my-4"/>
                  <div>
                    <h6 className="heading-small text-muted mt-4">
                      Matrix Notification Settings
                    </h6>
                  </div>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <Dropdown
                          name="Notifications"
                          value={matrixNotificationMode}
                          items={[{name: "Disabled", value: "Disabled"}, {
                            name: "Via direct messages",
                            value: "PrivateMessages"
                          }, {name: "Via room(s)", value: "Channels"}]}
                          onChange={newMode => setMatrixNotificationMode(newMode)}/>
                      </Col>
                      <Col lg="6">
                        {
                          matrixNotificationMode === "Channels"
                            ? <FormGroup>
                              <MultiDropdown
                                name="Room(s) to send notifications to"
                                create={true}
                                searchable={true}
                                placeholder="Enter room ids or aliases"
                                labelField="name"
                                valueField="id"
                                dropdownHandle={false}
                                selectedItems={matrixNotificationRooms.map(x => {
                                  return {name: x, id: x}
                                })}
                                items={matrixNotificationRooms.map(x => {
                                  return {name: x, id: x}
                                })}
                                onChange={newRooms => setMatrixNotificationRooms(newRooms.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                              <small className="form-text text-muted">
                                Room IDs (Room settings, Advanced, Internal room ID) or aliases. Invite the bot to each room. The bot mentions the requester there when their request is available.
                              </small>
                            </FormGroup>
                            : null
                        }
                      </Col>
                    </Row>
                    {
                      matrixNotificationMode !== "Disabled"
                        ? <Row>
                          <Col md="12">
                            <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                              <Input
                                className="custom-control-input"
                                id="matrixAutomaticRequests"
                                type="checkbox"
                                onChange={e => setMatrixAutomaticallyNotifyRequesters(!matrixAutomaticallyNotifyRequesters)}
                                checked={matrixAutomaticallyNotifyRequesters}
                              />
                              <label className="custom-control-label" htmlFor="matrixAutomaticRequests">
                                <span className="text-muted">Automatically notify users of downloaded content when they make requests</span>
                              </label>
                            </FormGroup>
                          </Col>
                        </Row>
                        : null
                    }
                  </div>
                  <hr className="my-4"/>
                  <h6 className="heading-small text-muted mb-4">
                    Matrix request limits
                  </h6>
                  <div className="pl-lg-4">
                    <p className="text-muted text-sm">
                      Pick which room power levels may request each type. Use exact levels below 50, for example 20 for movies only, 30 for movies and TV, 40 for everything.
                      Leave a type empty to let everyone request it. Power level 50 (moderator) and higher can always request everything.
                      Set someone's level in Element under Room settings, Roles and permissions.
                    </p>
                    <Row>
                      {
                        [
                          ["Movies", matrixMovieRequestLevels, setMatrixMovieRequestLevels],
                          ["TV shows", matrixTvShowRequestLevels, setMatrixTvShowRequestLevels],
                          ["Music", matrixMusicRequestLevels, setMatrixMusicRequestLevels],
                        ].map(([label, levels, setLevels]) => (
                          <Col lg="4" key={label}>
                            <FormGroup>
                              <MultiDropdown
                                name={`${label}: allowed power levels`}
                                create={true}
                                searchable={true}
                                placeholder="Everyone"
                                labelField="name"
                                valueField="id"
                                dropdownHandle={false}
                                selectedItems={levels.map(x => ({name: String(x), id: String(x)}))}
                                items={levels.map(x => ({name: String(x), id: String(x)}))}
                                onChange={newLevels => setLevels([...new Set(newLevels
                                  .map(x => String(x.id).trim())
                                  .filter(x => /^\d+$/.test(x) && Number(x) < 50)
                                  .map(Number))].sort((a, b) => a - b))}/>
                            </FormGroup>
                          </Col>
                        ))
                      }
                    </Row>
                    {
                      (() => {
                        const all = [...new Set([...matrixMovieRequestLevels, ...matrixTvShowRequestLevels, ...matrixMusicRequestLevels])].sort((a, b) => a - b);
                        if (!all.length) return null;
                        const types = level => [
                          (!matrixMovieRequestLevels.length || matrixMovieRequestLevels.includes(level)) ? "movies" : null,
                          (!matrixTvShowRequestLevels.length || matrixTvShowRequestLevels.includes(level)) ? "TV shows" : null,
                          (!matrixMusicRequestLevels.length || matrixMusicRequestLevels.includes(level)) ? "music" : null,
                        ].filter(x => x).join(", ");
                        return (
                          <div className="text-sm mb-3">
                            <div className="font-weight-bold mb-1">What each level can request</div>
                            {all.map(level => <div key={level}>Level {level}: {types(level)}</div>)}
                            <div>Level 50 and higher: everything</div>
                            <div className="text-muted">Any other level: {types(-1) || "nothing"}</div>
                          </div>);
                      })()
                    }
                  </div>
                  <hr className="my-4"/>
                  <h6 className="heading-small text-muted mb-4">
                    Matrix request approvals
                  </h6>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <MultiDropdown
                            name="Approval room(s)"
                            create={true}
                            searchable={true}
                            placeholder="Enter room ids or aliases"
                            labelField="name"
                            valueField="id"
                            dropdownHandle={false}
                            selectedItems={matrixApprovalRooms.map(x => {
                              return {name: x, id: x}
                            })}
                            items={matrixApprovalRooms.map(x => {
                              return {name: x, id: x}
                            })}
                            onChange={newRooms => setMatrixApprovalRooms(newRooms.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                        </FormGroup>
                      </Col>
                      <Col lg="6">
                        <p className="text-muted text-sm mt-4">
                          Requests that need approval in Seerr are posted here. People react with ✅ to approve or ❌ to deny.
                          Use room IDs from Room settings, Advanced, Internal room ID, and invite the bot.
                        </p>
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <label className="form-control-label" htmlFor="input-matrix-approval-power-level">
                            Power level for unlinked users
                          </label>
                          <Input
                            className="form-control-alternative"
                            id="input-matrix-approval-power-level"
                            type="number"
                            min="0"
                            max="100"
                            value={matrixApprovalPowerLevel}
                            onChange={e => setMatrixApprovalPowerLevel(e.target.value)}
                          />
                        </FormGroup>
                      </Col>
                      <Col lg="6">
                        <p className="text-muted text-sm mt-4">
                          People linked to a Seerr user on the Users page can approve when that Seerr user is an admin or has Manage Requests.
                          Unlinked people need this power level in the room: 50 is moderator, 100 is admin.
                        </p>
                      </Col>
                    </Row>
                    <Row>
                      <Col md="12">
                        <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                          <Input
                            className="custom-control-input"
                            id="matrixApprovalRoomsAllRequests"
                            type="checkbox"
                            onChange={() => setMatrixApprovalRoomsAllRequests(!matrixApprovalRoomsAllRequests)}
                            checked={matrixApprovalRoomsAllRequests}
                          />
                          <label className="custom-control-label" htmlFor="matrixApprovalRoomsAllRequests">
                            <span className="text-muted">Send all movie and tv show requests to the approval room(s), not only pending ones.</span>
                          </label>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                    </>
                  ) : null}
                  {discordEnabled ? (
                    <>
                  <hr className="my-4"/>
                  <div>
                    <h6 className="heading-small text-muted mb-4">
                      Discord Bot Settings
                    </h6>
                  </div>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <FormGroup className={clientIdInvalid ? "has-danger" : clientIdChanged ? "has-success" : ""}>
                          <label
                            className="form-control-label"
                            htmlFor="input-client-id"
                          >
                            Application Id
                          </label>
                          <Input
                            value={clientId} onChange={onClientIdChange}
                            className="form-control-alternative"
                            id="input-client-id"
                            placeholder="Enter client id"
                            type="text"
                          />
                          {
                            clientIdInvalid ? (
                                <Alert className="mt-3" color="warning">
                                  <strong>Client id is required.</strong>
                                </Alert>)
                              : null
                          }
                        </FormGroup>
                      </Col>
                      <Col lg="6">
                        <FormGroup className={botTokenInvalid ? "has-danger" : botTokenChanged ? "has-success" : ""}>
                          <label
                            className="form-control-label"
                            htmlFor="input-bot-token"
                          >
                            Bot Token
                          </label>
                          <Input
                            value={botToken} onChange={onBotTokenChange}
                            className="form-control-alternative"
                            id="input-bot-token"
                            placeholder="Enter bot token"
                            type="text"
                          />
                          {
                            botTokenInvalid ? (
                                <Alert className="mt-3" color="warning">
                                  <strong>Bot token is required.</strong>
                                </Alert>)
                              : null
                          }
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <MultiDropdown
                            name="Roles allowed to request tv shows"
                            create={true}
                            searchable={true}
                            placeholder="Enter role ids here. Leave blank for all roles."
                            labelField="name"
                            valueField="id"
                            dropdownHandle={false}
                            selectedItems={tvShowRoles.map(x => {
                              return {name: x, id: x}
                            })}
                            items={tvShowRoles.map(x => {
                              return {name: x, id: x}
                            })}
                            onChange={newTvShowRoles => setTvShowRoles(newTvShowRoles.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                        </FormGroup>
                      </Col>
                      <Col lg="6">
                        <FormGroup>
                          <MultiDropdown
                            name="Roles allowed to request movies"
                            create={true}
                            searchable={true}
                            placeholder="Enter role ids here. Leave blank for all roles."
                            labelField="name"
                            valueField="id"
                            dropdownHandle={false}
                            selectedItems={movieRoles.map(x => {
                              return {name: x, id: x}
                            })}
                            items={movieRoles.map(x => {
                              return {name: x, id: x}
                            })}
                            onChange={newMovieRoles => setMovieRoles(newMovieRoles.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <MultiDropdown
                            name="Roles allowed to request music"
                            create={true}
                            searchable={true}
                            placeholder="Enter role ids here. Leave blank for all roles."
                            labelField="name"
                            valueField="id"
                            dropdownHandle={false}
                            selectedItems={musicRoles.map(x => {
                              return {name: x, id: x}
                            })}
                            items={musicRoles.map(x => {
                              return {name: x, id: x}
                            })}
                            onChange={newMusicRoles => setMusicRoles(newMusicRoles.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col md="12">
                        <FormGroup>
                          <MultiDropdown
                            name="Channel(s) to monitor"
                            create={true}
                            searchable={true}
                            placeholder="Enter channels ids here. Leave blank for all channels."
                            labelField="name"
                            valueField="id"
                            dropdownHandle={false}
                            selectedItems={monitoredChannels.map(x => {
                              return {name: x, id: x}
                            })}
                            items={monitoredChannels.map(x => {
                              return {name: x, id: x}
                            })}
                            onChange={newMonitoredChannels => setMonitoredChannels(newMonitoredChannels.filter(x => /\S/.test(x.id)).map(x => x.id.trim().replace(/#/g, '').replace(/\s+/g, '-')))}/>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        <FormGroup>
                          <label
                            className="form-control-label"
                            htmlFor="input-status-message"
                          >
                            Status Message
                          </label>
                          <Input
                            value={statusMessage} onChange={onStatusMessageChange}
                            className="form-control-alternative"
                            id="input-status-message"
                            placeholder="Enter status message (Optional)"
                            type="text"
                          />
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                  <hr className="my-4"/>
                  <div>
                    <h6 className="heading-small text-muted mt-4">
                      Discord Request Approval Settings (Currently Seerr only) 
                    </h6>
                  </div>
                  <div className="pl-lg-4">
                    <Row>
                      <Col md="12">
                        <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                          <Input
                            className="custom-control-input"
                            id="adminChannelEnableApproval"
                            type="checkbox"
                            onChange={e => {
                              setRequestApprovalEnabled(x => !x);
                            }}
                            checked={requestApprovalEnabled}
                          />
                          <label
                            className="custom-control-label"
                            htmlFor="adminChannelEnableApproval"
                          >
                            <span
                              className="text-muted">Enable Approvals</span>
                          </label>
                        </FormGroup>
                      </Col>
                    </Row>
                    <span hidden={!requestApprovalEnabled}>
                      <Row>
                        <Col lg="6">
                          <FormGroup>
                            <MultiDropdown
                              name="Admin role ids for approvals"
                              create={true}
                              searchable={true}
                              placeholder="Enter discord role ids for request approvals."
                              labelField="name"
                              valueField="id"
                              dropdownHandle={false}
                              selectedItems={requestApprovalAdminRoleIds.map(x => {
                                return {name: x, id: x}
                              })}
                              items={requestApprovalAdminRoleIds.map(x => {
                                return {name: x, id: x}
                              })}
                              onChange={newAdminIds => setRequestApprovalAdminRoleIds(newAdminIds.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                          </FormGroup>
                        </Col>
                        <Col lg="6">
                          <FormGroup>
                            <MultiDropdown
                              name="Admin channel ids for request (approvals)"
                              create={true}
                              searchable={true}
                              placeholder="Enter discord channel ids for request approvals."
                              labelField="name"
                              valueField="id"
                              dropdownHandle={false}
                              selectedItems={requestApprovalAdminChannelIds.map(x => {
                                return {name: x, id: x}
                              })}
                              items={requestApprovalAdminChannelIds.map(x => {
                                return {name: x, id: x}
                              })}
                              onChange={newChannelIds => setRequestApprovalAdminChannelIds(newChannelIds.filter(x => /\S/.test(x.id)).map(x => x.id.trim()))}/>
                          </FormGroup>
                        </Col>
                        <Col md="12">
                          <FormGroup>
                            {
                              (!isLoading) && (
                                (() => {
                                  const warnings = [];
                                  if ((!requestApprovalAdminRoleIds || requestApprovalAdminRoleIds.length === 0) && (!requestApprovalAdminChannelIds || requestApprovalAdminChannelIds.length === 0)) {
                                    warnings.push("No request approval roles are filled. No requests can be seen or approved.");
                                  } else {
                                    if (!requestApprovalAdminRoleIds || requestApprovalAdminRoleIds.length === 0) {
                                      warnings.push("No admin role ids are configured. Requests that require approval will have no one able to approve/deny them.");
                                    }
                                    if (!requestApprovalAdminChannelIds || requestApprovalAdminChannelIds.length === 0) {
                                      warnings.push("No admin channel ids are configured. Pending approvals will not be posted to any admin channel.");
                                    }
                                  }
                                  if (warnings.length === 0) {
                                    return null;
                                  }
                                  return (
                                    <div className="mt-4">
                                      <Alert className="mb-0" color="warning">
                                        <strong>Approvals warning:</strong> {warnings.join(" ")}
                                      </Alert>
                                    </div>
                                  );
                                })()
                              )
                            }
                          </FormGroup>
                        </Col>
                        <Col lg="6">
                          <FormGroup>
                            <label
                              className="form-control-label"
                              htmlFor="input-approval-emoji-approve"
                            >
                              Approval emoji (Unicode only)
                            </label>
                            <Input
                              value={requestApprovalEmojiApprove}
                              onChange={e => setRequestApprovalEmojiApprove(e.target.value)}
                              className="form-control-alternative"
                              id="input-approval-emoji-approve"
                              placeholder="✅"
                              type="text"
                            />
                          </FormGroup>
                        </Col>
                        <Col lg="6">
                          <FormGroup>
                            <label
                              className="form-control-label"
                              htmlFor="input-approval-emoji-deny"
                            >
                              Deny emoji (Unicode only)
                            </label>
                            <Input
                              value={requestApprovalEmojiDeny}
                              onChange={e => setRequestApprovalEmojiDeny(e.target.value)}
                              className="form-control-alternative"
                              id="input-approval-emoji-deny"
                              placeholder="❌"
                              type="text"
                            />
                          </FormGroup>
                        </Col>
                      </Row>
                      
                      <Row>
                        <Col md="12">
                          <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                            <Input
                              className="custom-control-input"
                              id="adminChannelAllRequests"
                              type="checkbox"
                              onChange={e => {
                                setRequestApprovalAdminChannelAllRequests(!requestApprovalAdminChannelAllRequests);
                              }}
                              checked={requestApprovalAdminChannelAllRequests}
                            />
                            <label
                              className="custom-control-label"
                              htmlFor="adminChannelAllRequests"
                            >
                              <span
                                className="text-muted">Send all requests to admin channel(s), not only pending ones.</span>
                            </label>
                          </FormGroup>
                        </Col>
                      </Row>
                    </span>
                  </div>
                  <hr className="my-4"/>
                  <div>
                    <h6 className="heading-small text-muted mt-4">
                      Discord Notification Settings
                    </h6>
                  </div>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <Dropdown
                          name="Notifications"
                          value={notificationMode}
                          items={[{name: "Disabled", value: "Disabled"}, {
                            name: "Via private messages",
                            value: "PrivateMessages"
                          }, {name: "Via channel(s)", value: "Channels"}]}
                          onChange={newNotificationMode => setNotificationMode(newNotificationMode)}/>
                      </Col>
                      <Col lg="6">
                        {
                          notificationMode === "Channels"
                            ? <>
                              <FormGroup>
                                <MultiDropdown
                                  name="Channel(s) to send notifications to"
                                  create={true}
                                  searchable={true}
                                  placeholder="Enter channels ids here"
                                  labelField="name"
                                  valueField="id"
                                  dropdownHandle={false}
                                  selectedItems={notificationChannels.map(x => {
                                    return {name: x, id: x}
                                  })}
                                  items={notificationChannels.map(x => {
                                    return {name: x, id: x}
                                  })}
                                  onChange={newNotificationChannels => setNotificationChannels(newNotificationChannels.filter(x => /\S/.test(x.id)).map(x => x.id.trim().replace(/#/g, '').replace(/\s+/g, '-')))}/>
                              </FormGroup>
                            </>
                            : null
                        }
                      </Col>
                    </Row>
                    {
                      notificationMode !== "Disabled"
                        ? <>
                          <Row>
                            <Col md="12">
                              <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                                <Input
                                  className="custom-control-input"
                                  id="automaticRequests"
                                  type="checkbox"
                                  onChange={e => {
                                    setAutomaticallyNotifyRequesters(!automaticallyNotifyRequesters);
                                  }}
                                  checked={automaticallyNotifyRequesters}
                                />
                                <label
                                  className="custom-control-label"
                                  htmlFor="automaticRequests"
                                >
                                  <span className="text-muted">Automatically notify users of downloaded content when they make requests</span>
                                </label>
                              </FormGroup>
                            </Col>
                          </Row>
                        </>
                        : null
                    }
                  </div>
                  <div>
                    <h6 className="heading-small text-muted mt-4">
                      Discord Miscellaneous Settings
                    </h6>
                  </div>
                  <div className="pl-lg-4">
                    <Row>
                      <Col md="12">
                        <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                          <Input
                            className="custom-control-input"
                            id="enableRequestDM"
                            type="checkbox"
                            onChange={e => {
                              setEnableRequestsThroughDirectMessages(!enableRequestsThroughDirectMessages);
                            }}
                            checked={enableRequestsThroughDirectMessages}
                          />
                          <label
                            className="custom-control-label"
                            htmlFor="enableRequestDM"
                          >
                            <span className="text-muted">Enable requesting via a private message, all role restrictions will be ignored, even on the servers.<br/><strong>(It might take up to an hour for the commands to show up on your servers.)</strong></span>
                          </label>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col md="12">
                        <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
                          <Input
                            className="custom-control-input"
                            id="deleteRequestMessages"
                            type="checkbox"
                            onChange={e => {
                              setAutomaticallyPurgeCommandMessages(!automaticallyPurgeCommandMessages);
                            }}
                            checked={automaticallyPurgeCommandMessages}
                          />
                          <label
                            className="custom-control-label"
                            htmlFor="deleteRequestMessages"
                          >
                            <span className="text-muted">Hide slash command request messages in channels.</span>
                          </label>
                        </FormGroup>
                      </Col>
                    </Row>
                    <Row>
                      <Col md="12">
                      </Col>
                    </Row>
                  </div>
                  <div className="pl-lg-4">
                    <div className="mt-4">
                      {
                        testSettingsRequested && !isTestingSettings ?
                          !testSettingsSuccess ? (
                              <Alert className="text-center" color="danger">
                                <strong>{testSettingsError}.</strong>
                              </Alert>)
                            : <Alert className="text-center" color="success">
                              <strong>The specified settings are valid.</strong>
                            </Alert>
                          : null
                      }
                    </div>
                    <Row>
                      <Col>
                        <FormGroup className="text-right">
                          <Input id="discordlink" readOnly={true} className="d-none"
                                 value={"https://discord.com/api/oauth2/authorize?client_id=" + clientId + "&permissions=522304&scope=bot%20applications.commands"}/>
                          <button onClick={onTestSettings} disabled={!(validateBotToken() && validateClientId())}
                                  className="btn mt-3 btn-icon btn-3 btn-default" type="button">
                            <span className="btn-inner--icon">
                              {
                                isTestingSettings ? (
                                    <Oval
                                      wrapperClass="loader"
                                      type="Oval"
                                      color="#11cdef"
                                      height={19}
                                      width={19}
                                    />)
                                  : (<i className="fas fa-cogs"></i>)
                              }</span>
                            <span className="btn-inner--text">Test Settings</span>
                          </button>
                          <button onClick={onGenerateInviteLink} disabled={!(validateBotToken() && validateClientId())}
                                  className="btn mt-3 btn-icon btn-3 btn-info" type="button">
                            <span className="btn-inner--icon"><i className="fas fa-copy"></i></span>
                            <span
                              className="btn-inner--text">{isCopyingLink ? "Copied to clipboard!" : "Copy Invite Link"}</span>
                          </button>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                    </>
                  ) : null}
                  <hr className="my-4"/>
                  <h6 className="heading-small text-muted mb-4">
                    Chat Command Options
                  </h6>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <Dropdown
                          name="Language"
                          value={language}
                          items={availableLanguages.map(x => {
                            return {name: x, value: x}
                          })}
                          onChange={newLanguage => setLanguage(newLanguage)}/>
                      </Col>
                    </Row>
                    <Row>
                      <Col>
                        <FormGroup className="mt-4">
                          {
                            saveAttempted && !isSaving ?
                              !saveSuccess ? (
                                  <Alert className="text-center" color="danger">
                                    <strong>{saveError}</strong>
                                  </Alert>)
                                : <Alert className="text-center" color="success">
                                  <strong>Settings updated successfully.</strong>
                                </Alert>
                              : null
                          }
                        </FormGroup>
                        <FormGroup className="text-right">
                          <button className="btn btn-icon btn-3 btn-primary" onClick={onSaving} disabled={isSaving}
                                  type="button">
                            <span className="btn-inner--icon"><i className="fas fa-save"></i></span>
                            <span className="btn-inner--text">Save Changes</span>
                          </button>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                </Form>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default ChatClients;
