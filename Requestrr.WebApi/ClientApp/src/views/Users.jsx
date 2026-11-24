import { useEffect, useState } from "react";
import { useDispatch } from 'react-redux';
import { getLinkedUsers, saveLinkedUsers, getOverseerrUsers, getChatUsers } from "../store/actions/LinkedUsersActions";

// reactstrap components
import {
  Alert,
  Badge,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Input,
  InputGroup,
  Container,
  Row,
  Col,
  Table
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";

const PAGE_SIZE = 50;

const isMatrixId = value => /^@[^\s:]+:\S+$/.test(value);
const isDiscordId = value => /^\d{5,20}$/.test(value);


// A text box that suggests Discord and Matrix users the bot can see while you type; any typed ID still works.
function AccountPicker({ value, onChange, onPick, suggestions }) {
  const [isOpen, setIsOpen] = useState(false);
  const query = value.trim().toLowerCase();
  const matches = suggestions
    .filter(user => !query
      || user.name.toLowerCase().includes(query)
      || user.id.toLowerCase().includes(query)
      || (user.detail || "").toLowerCase().includes(query))
    .slice(0, 8);

  return (
    <div style={{position: "relative", flex: 1}}>
      <Input
        bsSize="sm"
        type="text"
        placeholder="Search users or paste an ID"
        value={value}
        onChange={e => { onChange(e.target.value); setIsOpen(true); }}
        onFocus={() => setIsOpen(true)}
        onBlur={() => setTimeout(() => setIsOpen(false), 150)}
        onKeyDown={e => {
          if (e.key === "Enter") {
            e.preventDefault();
            setIsOpen(false);
            onPick(matches.length === 1 && query && !isMatrixId(value.trim()) && !isDiscordId(value.trim()) ? matches[0].id : value.trim());
          } else if (e.key === "Escape") {
            setIsOpen(false);
          }
        }}
      />
      {
        isOpen && matches.length > 0 ? (
          <div className="dropdown-menu show w-100 py-1 mt-1" style={{position: "static", float: "none", maxHeight: "260px", overflowY: "auto"}}>
            {
              matches.map(user => (
                <button key={user.platform + user.id} type="button" className="dropdown-item py-2" style={{whiteSpace: "normal"}}
                        onMouseDown={e => e.preventDefault()}
                        onClick={() => { setIsOpen(false); onPick(user.id); }}>
                  <Badge color={user.platform === "Matrix" ? "success" : "primary"} className="mr-2" style={{textTransform: "none"}}>{user.platform}</Badge>
                  <span className="font-weight-bold">{user.name}</span>
                  <span className="text-muted text-sm ml-2">{user.detail}</span>
                </button>
              ))
            }
          </div>)
          : null
      }
    </div>
  );
}


function Users() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [loadError, setLoadError] = useState("");
  const [userLinks, setUserLinks] = useState([]);
  const [overseerrUsers, setOverseerrUsers] = useState([]);
  const [overseerrUsersError, setOverseerrUsersError] = useState("");
  const [search, setSearch] = useState("");
  const [newIds, setNewIds] = useState({});
  const [rowErrors, setRowErrors] = useState({});
  const [shownCount, setShownCount] = useState(PAGE_SIZE);
  const [chatUsers, setChatUsers] = useState([]);
  const [seerrConfigured, setSeerrConfigured] = useState(true);

  const dispatch = useDispatch();


  useEffect(() => {
    dispatch(getChatUsers())
      .then(data => setChatUsers(data.users || []));

    Promise.all([dispatch(getLinkedUsers()), dispatch(getOverseerrUsers())])
      .then(([links, users]) => {
        setIsLoading(false);

        if (links.ok) {
          setUserLinks(links.userLinks.map(x => ({chatUserId: x.chatUserId || "", overseerrUserId: x.overseerrUserId || ""})));
        } else {
          setLoadError(links.error);
        }

        if (users.ok) {
          setSeerrConfigured(users.seerrConfigured !== false);
          setOverseerrUsers(users.users);
        } else {
          setOverseerrUsersError(users.error);
        }
      });
  }, []);


  // Links to Seerr users that no longer exist (or could not be loaded) still get a row, so they can be removed.
  const knownIds = new Set(overseerrUsers.map(x => x.id));
  const rows = [
    ...overseerrUsers,
    ...[...new Set(userLinks.map(x => x.overseerrUserId))]
      .filter(id => !knownIds.has(id))
      .map(id => ({id: id, name: `Seerr user ${id}`, email: overseerrUsersError ? "" : "Not found in Seerr"}))
  ];

  const query = search.trim().toLowerCase();
  const linksOf = id => userLinks.filter(x => x.overseerrUserId === id);
  const filteredRows = !query ? rows : rows.filter(user =>
    user.name.toLowerCase().includes(query)
    || (user.email || "").toLowerCase().includes(query)
    || user.id === query
    || linksOf(user.id).some(link => link.chatUserId.toLowerCase().includes(query)));
  const shownRows = filteredRows.slice(0, shownCount);


  const setRowError = (id, error) => setRowErrors({...rowErrors, [id]: error});

  const chatUserName = chatUserId => {
    const user = chatUsers.find(x => x.id.toLowerCase() === chatUserId.toLowerCase());
    return user ? user.name : null;
  };

  const linkedIds = new Set(userLinks.map(x => x.chatUserId.toLowerCase()));
  const suggestions = chatUsers.filter(x => !linkedIds.has(x.id.toLowerCase()));

  const addLink = (id, picked) => {
    const chatUserId = (picked !== undefined ? picked : (newIds[id] || "")).trim();

    if (!isMatrixId(chatUserId) && !isDiscordId(chatUserId)) {
      setRowError(id, "Enter a Matrix ID (@name:your-domain) or a Discord user ID (a number).");
      return;
    }

    const existing = userLinks.find(x => x.chatUserId.toLowerCase() === chatUserId.toLowerCase());

    if (existing) {
      const owner = rows.find(x => x.id === existing.overseerrUserId);
      setRowError(id, `${chatUserId} is already linked to ${owner ? owner.name : "another Seerr user"}.`);
      return;
    }

    setUserLinks([...userLinks, {chatUserId: chatUserId, overseerrUserId: id}]);
    setNewIds({...newIds, [id]: ""});
    setRowError(id, "");
  };

  const removeLink = chatUserId => setUserLinks(userLinks.filter(x => x.chatUserId !== chatUserId));


  const onSaving = e => {
    e.preventDefault();

    if (isSaving) {
      return;
    }

    setIsSaving(true);

    dispatch(saveLinkedUsers(userLinks))
      .then(data => {
        setIsSaving(false);
        setSaveAttempted(true);
        setSaveSuccess(data.ok);
        setSaveError(data.ok ? "" : data.error);
      });
  };


  return (
    <>
      <UserHeader title="Users" description="This page is for linking Discord and Matrix accounts to Seerr users" />
      <Container className="mt--7" fluid>
        <Row>
          <Col className="order-xl-1" xl="12">
            <Card className="bg-secondary shadow">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Seerr users</h3>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody className={isLoading ? "fade" : "fade show"}>
                <Form className="complex" onSubmit={e => e.preventDefault()}>
                  <p className="text-muted text-sm">
                    Requests from a linked account are made as that Seerr user, so they show up under that person in Seerr and follow their permissions and quota.
                    A Seerr user can have several accounts, such as one on Discord and one on Matrix.
                    Type in the link box to search the members of the bot's Discord servers and Matrix rooms, or paste a Discord user ID or Matrix ID (@name:your-domain). A link here wins over the Discord ID saved in Seerr.
                    Unlinked accounts request as the default Seerr user set under Download Clients.
                    Users marked Admin have Admin or Manage Requests in Seerr, so their linked accounts can approve and deny requests in chat. Change this in Seerr.
                  </p>
                  {
                    loadError || overseerrUsersError ? (
                      <Alert className="mt-3" color="warning">
                        <strong>{loadError || overseerrUsersError}</strong>
                      </Alert>)
                      : null
                  }
                  {
                    !isLoading && !seerrConfigured ? (
                      <Alert color="info">
                        Linking needs Overseerr or Seerr as the movie or TV download client. With Radarr and Sonarr, requests go straight to download, so there are no Seerr users, permissions or approvals.
                        Chat rights then come from the Discord admin roles and the Matrix power level on the Chat clients page.
                      </Alert>)
                      : <>
                  <FormGroup>
                    <Input
                      className="form-control-alternative"
                      type="search"
                      placeholder="Search by name, email or linked account"
                      value={search}
                      onChange={e => { setSearch(e.target.value); setShownCount(PAGE_SIZE); }}
                    />
                  </FormGroup>
                  <div className="table-responsive">
                    <Table className="align-items-center table-flush bg-white" size="sm">
                      <thead className="thead-light">
                        <tr>
                          <th scope="col">Seerr user</th>
                          <th scope="col">Linked accounts</th>
                          <th scope="col" style={{minWidth: "360px"}}>Link an account</th>
                        </tr>
                      </thead>
                      <tbody>
                        {
                          shownRows.map(user => (
                            <tr key={user.id}>
                              <td>
                                <div className="font-weight-bold">
                                  {user.name}
                                  {
                                    user.canManageRequests
                                      ? <Badge color="warning" className="ml-2" style={{textTransform: "none"}} title="Admin or Manage Requests in Seerr: can approve and deny requests in chat">Admin</Badge>
                                      : null
                                  }
                                </div>
                                <div className="text-muted text-sm">{user.email}</div>
                              </td>
                              <td style={{whiteSpace: "normal"}}>
                                {
                                  linksOf(user.id).length === 0
                                    ? <span className="text-muted text-sm">None</span>
                                    : linksOf(user.id).map(link => (
                                      <Badge key={link.chatUserId} color={isMatrixId(link.chatUserId) ? "success" : "primary"} className="mr-2 mb-1" style={{fontSize: "0.8rem", textTransform: "none"}}>
                                        {isMatrixId(link.chatUserId) ? "Matrix" : "Discord"}: {chatUserName(link.chatUserId) ? `${chatUserName(link.chatUserId)} (${link.chatUserId})` : link.chatUserId}
                                        <button type="button" className="close ml-2" style={{float: "none", fontSize: "1rem", lineHeight: 1}}
                                                aria-label={`Unlink ${link.chatUserId}`} onClick={() => removeLink(link.chatUserId)}>
                                          <span aria-hidden="true">&times;</span>
                                        </button>
                                      </Badge>
                                    ))
                                }
                              </td>
                              <td>
                                <InputGroup size="sm" style={{alignItems: "flex-start", flexWrap: "nowrap"}}>
                                  <AccountPicker
                                    value={newIds[user.id] || ""}
                                    onChange={value => setNewIds({...newIds, [user.id]: value})}
                                    onPick={chatUserId => addLink(user.id, chatUserId)}
                                    suggestions={suggestions}
                                  />
                                  <button className="btn btn-sm btn-outline-primary ml-2" type="button" onClick={() => addLink(user.id)}>
                                    Link
                                  </button>
                                </InputGroup>
                                {
                                  rowErrors[user.id]
                                    ? <div className="text-danger text-sm mt-1" style={{whiteSpace: "normal"}}>{rowErrors[user.id]}</div>
                                    : null
                                }
                              </td>
                            </tr>
                          ))
                        }
                        {
                          !isLoading && filteredRows.length === 0
                            ? <tr><td colSpan="3" className="text-muted text-sm">{query ? "No Seerr users match your search." : "No Seerr users found."}</td></tr>
                            : null
                        }
                      </tbody>
                    </Table>
                  </div>
                  {
                    filteredRows.length > shownRows.length
                      ? <div className="text-center mt-3">
                        <button className="btn btn-sm btn-outline-primary" type="button" onClick={() => setShownCount(shownCount + PAGE_SIZE)}>
                          Show more ({filteredRows.length - shownRows.length} left)
                        </button>
                      </div>
                      : null
                  }
                      </>
                  }
                  <Row className="mt-4">
                    <Col>
                      <FormGroup>
                        {
                          saveAttempted && !isSaving ?
                            !saveSuccess ? (
                              <Alert className="text-center" color="danger">
                                <strong>{saveError}</strong>
                              </Alert>)
                              : <Alert className="text-center" color="success">
                                <strong>Linked accounts updated successfully.</strong>
                              </Alert>
                            : null
                        }
                      </FormGroup>
                      <FormGroup className="text-right">
                        <button className="btn btn-icon btn-3 btn-primary" onClick={onSaving} disabled={isSaving || isLoading || !!loadError} type="button">
                          <span className="btn-inner--icon"><i className="fas fa-save"></i></span>
                          <span className="btn-inner--text">Save Changes</span>
                        </button>
                      </FormGroup>
                    </Col>
                  </Row>
                </Form>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default Users;
